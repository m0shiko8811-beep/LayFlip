using System.Runtime.InteropServices;
using static LayFlip.App.NativeMethods;

namespace LayFlip.App;

/// <summary>Synthesizes keystrokes with SendInput using virtual keys (layout independent).</summary>
internal static class KeyboardInput
{
    private static readonly ushort[] ModifierKeys = [VK_SHIFT, VK_CONTROL, VK_MENU, VK_LWIN, VK_RWIN];

    public static bool IsDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    /// <summary>
    /// Waits until Ctrl, Alt, Shift, Win and the given extra key (0 for none) are all released.
    /// Does not block the calling thread. Returns false on timeout.
    /// </summary>
    public static async Task<bool> WaitForKeysReleasedAsync(int extraVk, int timeoutMs)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (true)
        {
            var anyDown = extraVk != 0 && IsDown(extraVk);
            foreach (var vk in ModifierKeys)
                anyDown |= IsDown(vk);

            if (!anyDown)
                return true;
            if (Environment.TickCount64 >= deadline)
                return false;
            await Task.Delay(10);
        }
    }

    /// <summary>Presses the modifier, taps the key, releases the modifier.</summary>
    public static void SendChord(ushort modifier, ushort key, bool keyIsExtended = false)
    {
        var keyFlags = keyIsExtended ? KEYEVENTF_EXTENDEDKEY : 0u;
        INPUT[] inputs =
        [
            Key(modifier, 0),
            Key(key, keyFlags),
            Key(key, keyFlags | KEYEVENTF_KEYUP),
            Key(modifier, KEYEVENTF_KEYUP),
        ];
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT Key(ushort vk, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags } },
    };
}
