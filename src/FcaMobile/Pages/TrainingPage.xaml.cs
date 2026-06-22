using Fca.Mobile.ViewModels;

namespace Fca.Mobile.Pages;

public partial class TrainingPage : ContentPage
{
    public TrainingPage(TrainingViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
