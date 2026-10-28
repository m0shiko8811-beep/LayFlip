using System.Runtime.InteropServices;

namespace LayFlip.App;

/// <summary>Clipboard access with retries, since another program may hold the clipboard open briefly.</summary>
internal static class ClipboardHelper
{
    private const int Attempts = 5;
    private const int RetryDelayMs = 50;

    public static T Retry<T>(Func<T> action)
    {
        for (var i = 1; ; i++)
        {
            try
            {
                return action();
            }
            catch (ExternalException) when (i < Attempts)
            {
                Thread.Sleep(RetryDelayMs);
            }
        }
    }

    public static void Retry(Action action) => Retry(() => { action(); return true; });

    /// <summary>Takes a detached copy of everything on the clipboard. Returns null if it was empty.</summary>
    public static DataObject? Snapshot()
    {
        return Retry(() =>
        {
            var live = Clipboard.GetDataObject();
            if (live is null)
                return null;

            var formats = live.GetFormats(false);
            if (formats.Length == 0)
                return null;

            var copy = new DataObject();
            foreach (var format in formats)
            {
                try
                {
                    var data = live.GetData(format, false);
                    if (data is not null)
                        copy.SetData(format, false, data);
                }
                catch
                {
                    // Some formats cannot be read back (delayed rendering, private handles). Skip them.
                }
            }
            return copy.GetFormats(false).Length > 0 ? copy : null;
        });
    }

    /// <summary>Puts the snapshot back, falling back to plain text if the full copy is refused.</summary>
    public static void Restore(DataObject? snapshot)
    {
        if (snapshot is null)
        {
            TryQuietly(() => Retry(Clipboard.Clear));
            return;
        }

        try
        {
            Retry(() => Clipboard.SetDataObject(snapshot, true));
        }
        catch
        {
            var text = snapshot.GetData(DataFormats.UnicodeText) as string;
            if (!string.IsNullOrEmpty(text))
                TryQuietly(() => Retry(() => Clipboard.SetText(text)));
        }
    }

    public static string? GetText()
    {
        return Retry(() => Clipboard.ContainsText() ? Clipboard.GetText() : null);
    }

    public static void SetText(string text) => Retry(() => Clipboard.SetText(text));

    private static void TryQuietly(Action action)
    {
        try { action(); } catch { }
    }
}
