using Fca.Mobile.Services;
using Fca.Mobile.Pages;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddSingleton(FcaConfig.Current);
        builder.Services.AddSingleton<IAppPreferences, MauiPreferences>();
        builder.Services.AddSingleton<ISecureCredentialStore, MauiSecureCredentialStore>();
        builder.Services.AddSingleton<INetworkStatus, MauiNetworkStatus>();
        builder.Services.AddSingleton<CustomerStore>();

        builder.Services.AddHttpClient<FcaApiClient>((sp, client) =>
        {
            var config = sp.GetRequiredService<FcaConfig>();
            client.BaseAddress = new Uri($"{config.PlatformBaseUrl.TrimEnd('/')}/api/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<WelcomePage>();
        builder.Services.AddTransient<SignInPage>();
        builder.Services.AddTransient<GetStartedPage>();
        builder.Services.AddTransient<CommandCenterPage>();
        builder.Services.AddTransient<LeadPipelinePage>();
        builder.Services.AddTransient<JobSitesPage>();
        builder.Services.AddTransient<TrainingPage>();
        builder.Services.AddTransient<PlanRoomPage>();
        builder.Services.AddTransient<InvoicesPage>();
        builder.Services.AddTransient<CommunicationsPage>();
        builder.Services.AddTransient<CustomerSuccessPage>();
        builder.Services.AddTransient<PlansPage>();
        builder.Services.AddTransient<AccountPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
