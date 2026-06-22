using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class SignInViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;
    private readonly MobileDeviceRegistrar _mobileRegistrar;
    private readonly IBiometricAuthService _biometrics;
    private readonly IShellNavigation _navigation;
    private readonly IHapticFeedbackService _haptics;

    public SignInViewModel(
        FcaApiClient api,
        MobileDeviceRegistrar mobileRegistrar,
        IBiometricAuthService biometrics,
        IConnectivityMonitor connectivity,
        IShellNavigation navigation,
        IHapticFeedbackService haptics)
        : base(connectivity)
    {
        _api = api;
        _mobileRegistrar = mobileRegistrar;
        _biometrics = biometrics;
        _navigation = navigation;
        _haptics = haptics;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    private string email = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    private string password = string.Empty;

    [ObservableProperty]
    private bool canUseBiometrics;

    [RelayCommand]
    private async Task InitializeAsync()
    {
        CanUseBiometrics = await _biometrics.IsAvailableAsync().ConfigureAwait(false);
    }

    private bool CanSignIn() =>
        !IsBusy && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _api.SignInAsync(Email.Trim(), Password).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                SetError(result.ErrorMessage ?? "Sign in failed.");
                return;
            }

            _haptics.Success();
            await _mobileRegistrar.RegisterIfNeededAsync(
                DeviceInfo.Current.Platform.ToString(),
                AppInfo.Current.VersionString,
                AppInfo.Current.PackageName).ConfigureAwait(false);

            if (CanUseBiometrics && !_biometrics.IsEnabled)
            {
                var enable = await Shell.Current.DisplayAlert(
                    "Enable biometric unlock?",
                    "Use Face ID or fingerprint to unlock FCA on this device.",
                    "Enable",
                    "Not now").ConfigureAwait(false);

                if (enable)
                    await _biometrics.SetEnabledAsync(true).ConfigureAwait(false);
            }

            await _navigation.GoToMainAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private Task GetStartedAsync()
    {
        _haptics.Click();
        return _navigation.GoToAsync("getstarted");
    }

    partial void OnIsBusyChanged(bool value) => SignInCommand.NotifyCanExecuteChanged();
}
