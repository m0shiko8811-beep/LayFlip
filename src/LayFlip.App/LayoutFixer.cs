using System.Diagnostics;
using LayFlip.Core;
using static LayFlip.App.NativeMethods;

namespace LayFlip.App;

/// <summary>
/// Grabs the selected text (or the current line up to the caret) from the foreground program
/// through the clipboard, converts it to the other layout and pastes it back.
/// Runs on the UI (STA) thread. All waiting is done with await Task.Delay so the message loop
/// keeps running: after a fix this app owns the clipboard, and when the target program copies,
/// Windows sends this app's clipboard window a message and makes the target wait for the reply.
/// Blocking the UI thread here would stall the target's Ctrl+C until the wait ended.
/// </summary>
internal static class LayoutFixer
{
    private const int CopyTimeoutMs = 400;
    private const int CopyRetryTimeoutMs = 1500;
    private const int KeyReleaseTimeoutMs = 3000;
    private const int ModifierGuardTimeoutMs = 500;
    private const int RestoreDelayMs = 300;
    private const int ClipboardQuietMs = 60;
    private const int ClipboardQuietMaxMs = 500;
    private const int PollMs = 10;

    public static async Task RunAsync(IntPtr foregroundWindow, HotkeyPreset hotkey, Action<string> reportProblem)
    {
        if (!await KeyboardInput.WaitForKeysReleasedAsync((int)hotkey.VirtualKey, KeyReleaseTimeoutMs))
        {
            DiagnosticLog.Write($"keys still held after {KeyReleaseTimeoutMs} ms, not sending any keys");
            reportProblem("Keys were still held down. Release all keys after the hotkey and try again.");
            return;
        }

        DataObject? saved;
        try
        {
            saved = ClipboardHelper.Snapshot();
        }
        catch (Exception ex)
        {
            DiagnosticLog.Write($"clipboard snapshot failed: {ex.GetType().Name}");
            reportProblem("The clipboard is busy right now. Please try again in a moment.");
            return;
        }

        try
        {
            var original = await CopyFromForegroundAsync();
            if (string.IsNullOrEmpty(original))
            {
                ClipboardHelper.Restore(saved);
                DiagnosticLog.Write("restored (nothing to fix)");
                return;
            }

            var result = LayoutConverter.Convert(original);
            if (result.Text == original)
            {
                DiagnosticLog.Write("converted: no change, nothing to paste");
                ClipboardHelper.Restore(saved);
                DiagnosticLog.Write("restored");
                return;
            }
            DiagnosticLog.Write($"converted {result.Direction}, length {result.Text.Length}");

            // The copying program may still be adding formats. If it wrote after us,
            // it would replace the converted text and the original would be pasted back.
            await WaitForClipboardQuietAsync();
            var ours = await PutOnClipboardAsync(result.Text);
            await SendChordAsync(VK_CONTROL, VK_V);
            DiagnosticLog.Write("pasted (Ctrl+V sent)");

            // Give the target time to read the converted text before the user's clipboard comes back.
            await Task.Delay(RestoreDelayMs);
            DiagnosticLog.Write($"waited {RestoreDelayMs} ms after Ctrl+V, restoring");
            var now = GetClipboardSequenceNumber();
            if (now != ours)
                DiagnosticLog.Write($"warning: clipboard changed by another program during the paste (seq {ours} to {now})");

            ClipboardHelper.Restore(saved);
            DiagnosticLog.Write("restored");

            var layout = InputLanguageSwitcher.SwitchTo(foregroundWindow, result.Direction);
            DiagnosticLog.Write(layout == IntPtr.Zero
                ? "layout switch skipped (target layout not installed or no window)"
                : $"layout switched to 0x{(long)layout & 0xFFFF:X4}");
        }
        catch (Exception ex)
        {
            DiagnosticLog.Write($"error: {ex.GetType().Name}: {ex.Message}");
            ClipboardHelper.Restore(saved);
            reportProblem("Could not fix the text this time. Please try again.");
        }
    }

    /// <summary>
    /// Copies the selection. If nothing at all is copied, selects the line up to the caret and copies that.
    /// If that still yields no text, the copy is retried once with a longer timeout before giving up.
    /// Returns null when no text could be copied.
    /// </summary>
    private static async Task<string?> CopyFromForegroundAsync()
    {
        var copy = await CopyAsync(CopyTimeoutMs, "selection");
        if (copy.Text is not null)
            return copy.Text;

        if (!copy.Changed)
        {
            // The target did not copy anything, so most likely nothing is selected.
            // Never done after the target did copy, so a real selection is not altered.
            await SendChordAsync(VK_SHIFT, VK_HOME, keyIsExtended: true);
            copy = await CopyAsync(CopyTimeoutMs, "line");
            if (copy.Text is not null)
                return copy.Text;
        }

        copy = await CopyAsync(CopyRetryTimeoutMs, "retry");
        if (copy.Text is null)
            DiagnosticLog.Write("copy gave up: no text");
        return copy.Text;
    }

    /// <summary>
    /// Sends Ctrl+C and waits for text to arrive. Changed is true if the clipboard changed at all,
    /// which means the target did copy something even if no text could be read in time.
    /// </summary>
    private static async Task<(bool Changed, string? Text)> CopyAsync(int timeoutMs, string attempt)
    {
        var before = GetClipboardSequenceNumber();
        var owner = DiagnosticLog.Enabled ? WindowProcessName(GetClipboardOwner()) : "";
        var clock = Stopwatch.StartNew();
        await SendChordAsync(VK_CONTROL, VK_C);

        while (GetClipboardSequenceNumber() == before)
        {
            if (clock.ElapsedMilliseconds >= timeoutMs)
            {
                DiagnosticLog.Write($"copy ({attempt}): timeout after {clock.ElapsedMilliseconds} ms, nothing copied (clipboard owner was {owner})");
                return (false, null);
            }
            await Task.Delay(PollMs);
        }
        var changedAfter = clock.ElapsedMilliseconds;

        // The copying program empties the clipboard first and adds its formats afterwards,
        // so text can show up a little after the sequence number moves. Keep reading until it does.
        while (true)
        {
            await Task.Delay(20);
            var text = ClipboardHelper.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                DiagnosticLog.Write($"copy ({attempt}): text length {text.Length}, clipboard changed after {changedAfter} ms, text read after {clock.ElapsedMilliseconds} ms (clipboard owner was {owner})");
                return (true, text);
            }
            if (clock.ElapsedMilliseconds >= timeoutMs)
            {
                DiagnosticLog.Write($"copy ({attempt}): timeout, clipboard changed after {changedAfter} ms but no text by {clock.ElapsedMilliseconds} ms (clipboard owner was {owner})");
                return (true, null);
            }
        }
    }

    /// <summary>Waits until the clipboard has not changed for a short while (bounded).</summary>
    private static async Task WaitForClipboardQuietAsync()
    {
        var clock = Stopwatch.StartNew();
        var seq = GetClipboardSequenceNumber();
        var quietSince = 0L;
        while (clock.ElapsedMilliseconds - quietSince < ClipboardQuietMs && clock.ElapsedMilliseconds < ClipboardQuietMaxMs)
        {
            await Task.Delay(PollMs);
            var now = GetClipboardSequenceNumber();
            if (now != seq)
            {
                DiagnosticLog.Write($"clipboard still changing after the copy (seq {seq} to {now}) at {clock.ElapsedMilliseconds} ms");
                seq = now;
                quietSince = clock.ElapsedMilliseconds;
            }
        }
    }

    /// <summary>
    /// Puts the converted text on the clipboard and checks that nobody replaced it right away.
    /// Plain, fully rendered text (copied into the clipboard), as ClipboardHelper.SetText does.
    /// Returns the clipboard sequence number of our text.
    /// </summary>
    private static async Task<uint> PutOnClipboardAsync(string text)
    {
        for (var attempt = 1; ; attempt++)
        {
            ClipboardHelper.SetText(text);
            var ours = GetClipboardSequenceNumber();
            await Task.Delay(PollMs);
            var now = GetClipboardSequenceNumber();
            if (now == ours || attempt == 3)
                return now;
            DiagnosticLog.Write($"converted text was replaced on the clipboard (seq {ours} to {now}), setting it again");
        }
    }

    /// <summary>
    /// Sends a chord, first waiting briefly for modifiers to be up so our synthesized
    /// key-up cannot cut into a shortcut the user (or another program) is pressing.
    /// </summary>
    private static async Task SendChordAsync(ushort modifier, ushort key, bool keyIsExtended = false)
    {
        if (!await KeyboardInput.WaitForKeysReleasedAsync(0, ModifierGuardTimeoutMs))
            DiagnosticLog.Write("a modifier was down before sending a chord, sending anyway");
        KeyboardInput.SendChord(modifier, key, keyIsExtended);
    }

    private static string WindowProcessName(IntPtr window)
    {
        if (window == IntPtr.Zero)
            return "none";
        GetWindowThreadProcessId(window, out var pid);
        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        }
        catch
        {
            return $"pid {pid}";
        }
    }
}
