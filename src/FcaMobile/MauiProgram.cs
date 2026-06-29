using Fca.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"Unhandled: {e.ExceptionObject}");
        };

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddSingleton(FcaConfig.Current);
        builder.Services.AddSingleton(FcaMediaConfig.Current);
        builder.Services.AddSingleton<CustomerStore>();
        builder.Services.AddSingleton<FcaApiClient>();

        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<Pages.WelcomePage>();
        builder.Services.AddTransient<Pages.SignInPage>();
        builder.Services.AddTransient<Pages.GetStartedPage>();
        builder.Services.AddTransient<Pages.CommandCenterPage>();
        builder.Services.AddTransient<Pages.LeadPipelinePage>();
        builder.Services.AddTransient<Pages.JobSitesPage>();
        builder.Services.AddTransient<Pages.TrainingPage>();
        builder.Services.AddTransient<Pages.PlanRoomPage>();
        builder.Services.AddTransient<Pages.InvoicesPage>();
        builder.Services.AddTransient<Pages.CommunicationsPage>();
        builder.Services.AddTransient<Pages.CustomerSuccessPage>();
        builder.Services.AddTransient<Pages.PlansPage>();
        builder.Services.AddTransient<Pages.AccountPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
