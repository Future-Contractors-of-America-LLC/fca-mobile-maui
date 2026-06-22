using Fca.Mobile.Pages;
using Fca.Mobile.Services;

namespace Fca.Mobile;

public partial class AppShell : Shell
{
    private readonly CustomerStore _store;
    private readonly FcaApiClient _api;
    private readonly IBiometricAuthService _biometrics;

    public AppShell(
        CustomerStore store,
        FcaApiClient api,
        IBiometricAuthService biometrics,
        WelcomePage welcome,
        CommandCenterPage commandCenter,
        LeadPipelinePage leads,
        JobSitesPage jobs,
        TrainingPage training,
        AccountPage account)
    {
        _store = store;
        _api = api;
        _biometrics = biometrics;
        InitializeComponent();

        WelcomeShell.Content = welcome;
        CommandShell.Content = commandCenter;
        LeadsShell.Content = leads;
        JobsShell.Content = jobs;
        TrainingShell.Content = training;
        AccountShell.Content = account;

        Routing.RegisterRoute("signin", typeof(SignInPage));
        Routing.RegisterRoute("getstarted", typeof(GetStartedPage));
        Routing.RegisterRoute("plans", typeof(PlansPage));
        Routing.RegisterRoute("planroom", typeof(PlanRoomPage));
        Routing.RegisterRoute("invoices", typeof(InvoicesPage));
        Routing.RegisterRoute("communications", typeof(CommunicationsPage));
        Routing.RegisterRoute("support", typeof(CustomerSuccessPage));

        Loaded += OnShellLoaded;
        Navigating += OnShellNavigating;
    }

    async void OnShellLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnShellLoaded;

        if (_store.IsSignedIn)
        {
            if (_biometrics.IsEnabled)
            {
                var unlocked = await _biometrics.AuthenticateAsync("Unlock your FCA workspace.").ConfigureAwait(false);
                if (!unlocked)
                {
                    await GoToAsync("//welcome").ConfigureAwait(false);
                    return;
                }
            }

            var session = await _api.SyncSessionAsync().ConfigureAwait(false);
            if (!session.IsSuccess)
            {
                await _api.SignOutAsync().ConfigureAwait(false);
                await GoToAsync("//welcome").ConfigureAwait(false);
                return;
            }
        }

        var route = _store.IsSignedIn ? "//main/command" : "//welcome";
        await GoToAsync(route).ConfigureAwait(false);
    }

    void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (_store.IsSignedIn || !RequiresAuthentication(e.Target.Location.OriginalString))
            return;

        e.Cancel();
        Dispatcher.Dispatch(async () => await GoToAsync("//welcome").ConfigureAwait(false));
    }

    private static bool RequiresAuthentication(string location) =>
        location.Contains("/main/", StringComparison.OrdinalIgnoreCase);
}
