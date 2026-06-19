using System.Diagnostics;

namespace Fca.Mobile;

public partial class App : Application
{
    public App(AppShell shell)
    {
        InitializeComponent();
        HookGlobalExceptionHandlers();
        MainPage = shell;
    }

    private static void HookGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Debug.WriteLine($"[FCA] Unhandled exception: {e.ExceptionObject}");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Debug.WriteLine($"[FCA] Unobserved task exception: {e.Exception}");
            e.SetObserved();
        };
    }
}
