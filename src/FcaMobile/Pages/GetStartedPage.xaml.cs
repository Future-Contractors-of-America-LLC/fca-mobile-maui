using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class GetStartedPage : ContentPage
{
    public GetStartedPage(GetStartedViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
