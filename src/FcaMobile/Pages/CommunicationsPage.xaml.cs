using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class CommunicationsPage : ContentPage
{
    public CommunicationsPage(CommunicationsViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
