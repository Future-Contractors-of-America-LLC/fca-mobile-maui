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

        CurrentItem = WelcomeShell;
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        if (args.Source == ShellNavigationSource.ShellItemChanged)
            return;

        var target = args.Target?.Location?.OriginalString ?? string.Empty;
        if (_store.IsSignedIn || !IsProtectedRoute(target))
            return;

        args.Cancel();
        Dispatcher.Dispatch(async () =>
        {
            try
            {
                await GoToAsync("//welcome");
            }
            catch
            {
                // Ignore re-entrant navigation during shell bootstrap.
            }
        });
    }

    private static bool IsProtectedRoute(string route)
    {
        return route.Contains("//main", StringComparison.OrdinalIgnoreCase) ||
               route.Contains("/main/", StringComparison.OrdinalIgnoreCase);
    }
}
