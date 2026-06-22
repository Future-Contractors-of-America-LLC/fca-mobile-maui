using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class AccountPage : ContentPage
{
    public AccountPage(AccountViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
