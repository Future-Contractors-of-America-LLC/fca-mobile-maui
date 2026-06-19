using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class PlansViewModel : ViewModelBase
{
    private readonly IShellNavigation _navigation;
    private readonly IHapticFeedbackService _haptics;

    public PlansViewModel(
        IConnectivityMonitor connectivity,
        IShellNavigation navigation,
        IHapticFeedbackService haptics)
        : base(connectivity)
    {
        _navigation = navigation;
        _haptics = haptics;
    }

    [RelayCommand]
    private Task GetStartedAsync()
    {
        _haptics.Click();
        return _navigation.GoToAsync("getstarted");
    }
}
