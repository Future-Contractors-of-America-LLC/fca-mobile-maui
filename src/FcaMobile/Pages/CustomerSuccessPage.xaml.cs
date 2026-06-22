using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class CustomerSuccessPage : ContentPage
{
    public CustomerSuccessPage(CustomerSuccessViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
