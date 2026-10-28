using static LayFlip.App.NativeMethods;

namespace LayFlip.App;

/// <summary>Hidden message window that owns the global hotkey registration.</summary>
internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int HotkeyId = 1;
    private bool _registered;

    public event Action? HotkeyPressed;

    public bool IsRegistered => _registered;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    public bool Register(HotkeyPreset preset)
    {
        Unregister();
        _registered = RegisterHotKey(Handle, HotkeyId, preset.Modifiers | MOD_NOREPEAT, preset.VirtualKey);
        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(Handle, HotkeyId);
            _registered = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && (int)m.WParam == HotkeyId)
        {
            HotkeyPressed?.Invoke();
            return;
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
    }
}
