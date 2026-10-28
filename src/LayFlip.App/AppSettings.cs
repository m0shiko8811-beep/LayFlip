using System.Text.Json;

namespace LayFlip.App;

/// <summary>User settings stored in %APPDATA%\LayFlip\settings.json.</summary>
internal sealed class AppSettings
{
    public string Hotkey { get; set; } = HotkeyPreset.Default.Id;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LayFlip", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            // A damaged settings file should never stop the app; fall back to defaults.
        }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
