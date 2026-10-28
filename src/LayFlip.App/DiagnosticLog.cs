using System.Diagnostics;

namespace LayFlip.App;

/// <summary>
/// Opt-in step log for troubleshooting. Enabled only when the environment variable
/// LAYFLIP_LOG is set to 1. Lines are appended to %TEMP%\LayFlip.log.
/// </summary>
internal static class DiagnosticLog
{
    private static readonly object Gate = new();
    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    public static bool Enabled { get; } = Environment.GetEnvironmentVariable("LAYFLIP_LOG") == "1";

    public static string FilePath { get; } = Path.Combine(Path.GetTempPath(), "LayFlip.log");

    public static void Write(string message)
    {
        if (!Enabled)
            return;

        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} +{Clock.ElapsedMilliseconds,7}ms [t{Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}";
        try
        {
            lock (Gate)
                File.AppendAllText(FilePath, line);
        }
        catch
        {
            // Logging must never break a fix.
        }
    }
}
