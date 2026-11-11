namespace ShiningEditor;

internal static class Program
{
    /// <summary>The main entry point for the application.</summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Log any unhandled exception so failures are diagnosable instead of a silent crash.
        Application.ThreadException += (_, e) => Game.Log.Write("Unhandled: " + e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Game.Log.Write("Unhandled: " + (e.ExceptionObject?.ToString() ?? "unknown"));

        Application.Run(new MainForm());
    }
}
