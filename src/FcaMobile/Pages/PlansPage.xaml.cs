using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class PlansPage : ContentPage
{
    public PlansPage(PlansViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
