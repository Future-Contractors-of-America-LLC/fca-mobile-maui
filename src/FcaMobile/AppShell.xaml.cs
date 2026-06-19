using Fca.Mobile.Pages;
using Fca.Mobile.Services;

namespace Fca.Mobile;

public partial class AppShell : Shell
{
    private readonly CustomerStore _store;

    public AppShell(
        WelcomePage welcome,
        CommandCenterPage commandCenter,
        LeadPipelinePage leads,
        JobSitesPage jobs,
        TrainingPage training,
        AccountPage account,
        CustomerStore store)
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
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        if (!_store.IsSignedIn && IsProtectedRoute(args.Target.Location.OriginalString))
        {
            args.Cancel();
            Dispatcher.Dispatch(async () => await GoToAsync("//welcome"));
            return;
        }

        base.OnNavigating(args);
    }

    public void RestoreSession()
    {
        if (_store.IsSignedIn)
            Dispatcher.Dispatch(async () => await GoToAsync("//main/command"));
    }

    private static bool IsProtectedRoute(string location)
        => location.Contains("//main", StringComparison.OrdinalIgnoreCase)
           || location.Contains("/main/", StringComparison.OrdinalIgnoreCase);
}
