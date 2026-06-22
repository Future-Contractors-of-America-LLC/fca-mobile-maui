using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class PlanRoomPage : ContentPage
{
    public PlanRoomPage(PlanRoomViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
