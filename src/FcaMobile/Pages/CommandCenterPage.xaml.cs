using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class CommandCenterPage : ContentPage
{
    public CommandCenterPage(CommandCenterViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
