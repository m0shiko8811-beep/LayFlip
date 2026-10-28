namespace LayFlip.App;

internal static class Program
{
    // Kept in a static field so the single-instance lock lives as long as the process.
    private static Mutex? _instanceMutex;

    [STAThread]
    private static void Main()
    {
        _instanceMutex = new Mutex(initiallyOwned: true, @"Local\LayFlip.SingleInstance", out var createdNew);
        if (!createdNew)
            return;

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new TrayContext());
        }
        finally
        {
            _instanceMutex.ReleaseMutex();
            _instanceMutex.Dispose();
        }
    }
}
