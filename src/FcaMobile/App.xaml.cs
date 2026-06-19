using Microsoft.Extensions.Logging;

namespace Fca.Mobile;

public partial class App : Application
{
    private readonly ILogger<App> _logger;

    public App(AppShell shell, ILogger<App> logger)
    {
        _logger = logger;
        InitializeComponent();
        UserAppTheme = AppTheme.Unspecified;
        MainPage = shell;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            _logger.LogCritical(ex, "Unhandled application exception");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }
}
