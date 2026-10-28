using System.Drawing;
using static LayFlip.App.NativeMethods;

namespace LayFlip.App;

/// <summary>The whole app: a tray icon, its menu and the hotkey. There is no main window.</summary>
internal sealed class TrayContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly HotkeyWindow _hotkeyWindow = new();
    private readonly Icon _icon;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _hotkeyMenu = new();
    private readonly ToolStripMenuItem _startupItem = new("Start with Windows");
    private HotkeyPreset _hotkey;
    private bool _running;
    private bool _pending;
    private IntPtr _pendingWindow;

    public TrayContext()
    {
        _settings = AppSettings.Load();
        _hotkey = HotkeyPreset.FromId(_settings.Hotkey);
        _icon = TrayIconFactory.Create();

        foreach (var preset in HotkeyPreset.All)
        {
            var item = new ToolStripMenuItem(preset.DisplayName) { Tag = preset };
            item.Click += (_, _) => ChangeHotkey((HotkeyPreset)item.Tag!);
            _hotkeyMenu.DropDownItems.Add(item);
        }

        _startupItem.Checked = SafeIsStartupEnabled();
        _startupItem.Click += (_, _) => ToggleStartup();

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitThread();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_hotkeyMenu);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            ContextMenuStrip = menu,
            Visible = true,
        };

        _hotkeyWindow.HotkeyPressed += OnHotkeyPressed;

        if (!_hotkeyWindow.Register(_hotkey))
            ShowProblem($"{_hotkey.DisplayName} is already used by another program. Right-click the tray icon to pick a different key.");

        RefreshHotkeyText();
    }

    private async void OnHotkeyPressed()
    {
        // Capture the target window first, before anything else can take focus.
        var foreground = GetForegroundWindow();

        // A press during a running fix is never dropped: it is remembered (at most one) and
        // run as soon as the current fix finishes.
        if (_running)
        {
            _pendingWindow = foreground;
            _pending = true;
            DiagnosticLog.Write("press received while a fix is running, queued");
            return;
        }

        DiagnosticLog.Write("press received");
        _running = true;
        try
        {
            var target = foreground;
            while (true)
            {
                try
                {
                    await LayoutFixer.RunAsync(target, _hotkey, ShowProblem);
                }
                catch (Exception ex)
                {
                    // Nothing may escape an async void handler.
                    DiagnosticLog.Write($"fix failed: {ex.GetType().Name}: {ex.Message}");
                    ShowProblem("Could not fix the text this time. Please try again.");
                }

                if (!_pending)
                    break;
                _pending = false;
                target = _pendingWindow;
                DiagnosticLog.Write("running the queued press");
            }
        }
        finally
        {
            _running = false;
            DiagnosticLog.Write("idle");
        }
    }

    private void ChangeHotkey(HotkeyPreset preset)
    {
        if (preset == _hotkey && _hotkeyWindow.IsRegistered)
            return;

        var previous = _hotkey;
        if (_hotkeyWindow.Register(preset))
        {
            _hotkey = preset;
            _settings.Hotkey = preset.Id;
            try
            {
                _settings.Save();
            }
            catch
            {
                ShowProblem("The new key works, but it could not be saved. It will reset after a restart.");
            }
        }
        else if (preset == previous)
        {
            ShowProblem($"{preset.DisplayName} is still used by another program. Please pick a different key.");
        }
        else
        {
            var restored = _hotkeyWindow.Register(previous);
            ShowProblem(restored
                ? $"{preset.DisplayName} is already used by another program. Keeping {previous.DisplayName}."
                : $"{preset.DisplayName} is already used by another program. Please pick a different key.");
        }
        RefreshHotkeyText();
    }

    private void ToggleStartup()
    {
        var enable = !_startupItem.Checked;
        try
        {
            StartupRegistration.SetEnabled(enable);
            _startupItem.Checked = enable;
        }
        catch
        {
            ShowProblem("Could not change the Start with Windows setting.");
        }
    }

    private static bool SafeIsStartupEnabled()
    {
        try { return StartupRegistration.IsEnabled(); }
        catch { return false; }
    }

    private void RefreshHotkeyText()
    {
        _hotkeyMenu.Text = $"Hotkey: {_hotkey.DisplayName}";
        foreach (ToolStripMenuItem item in _hotkeyMenu.DropDownItems)
            item.Checked = Equals(item.Tag, _hotkey);

        // Tooltip text is limited to 63 characters.
        _notifyIcon.Text = $"LayFlip: fix keyboard layout ({_hotkey.DisplayName})";
    }

    private void ShowProblem(string message)
    {
        _notifyIcon.ShowBalloonTip(5000, "LayFlip", message, ToolTipIcon.Warning);
    }

    protected override void ExitThreadCore()
    {
        _hotkeyWindow.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
        base.ExitThreadCore();
    }
}
