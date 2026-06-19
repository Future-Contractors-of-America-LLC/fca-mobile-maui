using Fca.Mobile.Pages;
using Fca.Mobile.Services;

namespace Fca.Mobile;

public partial class AppShell : Shell
{
    private readonly CustomerStore _store;

    public AppShell(
        CustomerStore store,
        WelcomePage welcome,
        CommandCenterPage commandCenter,
        LeadPipelinePage leads,
        JobSitesPage jobs,
        TrainingPage training,
        AccountPage account)
    {
        _store = store;
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
