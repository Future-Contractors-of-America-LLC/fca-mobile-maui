using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class WelcomePage : ContentPage
{
    public WelcomePage(WelcomeViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
