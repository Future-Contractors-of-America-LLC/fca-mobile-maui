using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class LeadPipelinePage : ContentPage
{
    public LeadPipelinePage(LeadPipelineViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
