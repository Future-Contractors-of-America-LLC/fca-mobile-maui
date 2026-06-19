using Fca.Mobile.Pages;

namespace Fca.Mobile;

public partial class AppShell : Shell
{
    public AppShell(
        WelcomePage welcome,
        CommandCenterPage commandCenter,
        LeadPipelinePage leads,
        JobSitesPage jobs,
        TrainingPage training,
        AccountPage account)
    {
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
}
