using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class JobSitesPage : ContentPage
{
    public JobSitesPage(JobSitesViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
