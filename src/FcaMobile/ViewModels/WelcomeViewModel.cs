using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    private readonly IShellNavigation _navigation;
    private readonly IHapticFeedbackService _haptics;

    public WelcomeViewModel(
        IConnectivityMonitor connectivity,
        IShellNavigation navigation,
        IHapticFeedbackService haptics)
        : base(connectivity)
    {
        _navigation = navigation;
        _haptics = haptics;
    }

    [RelayCommand]
    private Task ViewPlansAsync()
    {
        _haptics.Click();
        return _navigation.GoToAsync("plans");
    }

    [RelayCommand]
    private Task SignInAsync()
    {
        _haptics.Click();
        return _navigation.GoToAsync("signin");
    }

    [RelayCommand]
    private Task GetStartedAsync()
    {
        _haptics.Click();
        return _navigation.GoToAsync("getstarted");
    }
}
