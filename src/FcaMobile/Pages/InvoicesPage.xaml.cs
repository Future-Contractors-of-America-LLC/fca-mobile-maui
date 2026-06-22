using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class InvoicesPage : ContentPage
{
    public InvoicesPage(InvoicesViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
