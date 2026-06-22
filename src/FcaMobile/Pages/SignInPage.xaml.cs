using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class SignInPage : ContentPage
{
    public SignInPage(SignInViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
