using Microsoft.Win32;

namespace LayFlip.App;

/// <summary>Manages the "Start with Windows" entry under HKCU\...\Run.</summary>
internal static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LayFlip";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string;
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
            key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
