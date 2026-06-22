using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class AccountViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;
    private readonly FcaConfig _config;
    private readonly IFcaApiHostResolver _hostResolver;
    private readonly IBiometricAuthService _biometrics;
    private readonly IShellNavigation _navigation;
    private readonly IHapticFeedbackService _haptics;

    public AccountViewModel(
        FcaApiClient api,
        CustomerStore store,
        FcaConfig config,
        IFcaApiHostResolver hostResolver,
        IBiometricAuthService biometrics,
        IConnectivityMonitor connectivity,
        IShellNavigation navigation,
        IHapticFeedbackService haptics)
        : base(connectivity)
    {
        _api = api;
        _store = store;
        _config = config;
        _hostResolver = hostResolver;
        _biometrics = biometrics;
        _navigation = navigation;
        _haptics = haptics;
        AppVersion = $"Version {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";
    }

    [ObservableProperty]
    private string company = "Company not set";

    [ObservableProperty]
    private string email = "Not signed in";

    [ObservableProperty]
    private string plan = "Plan: startup";

    [ObservableProperty]
    private string productsSummary = string.Empty;

    [ObservableProperty]
    private string commsSummary = string.Empty;

    [ObservableProperty]
    private string apiHostLabel = string.Empty;

    [ObservableProperty]
    private string appVersion = string.Empty;

    [ObservableProperty]
    private bool biometricUnlockEnabled;

    [ObservableProperty]
    private bool biometricAvailable;

    [ObservableProperty]
    private string biometricLabel = "Biometric unlock";

    private bool _suppressBiometricCallback;

    [RelayCommand]
    private async Task InitializeAsync()
    {
        var profile = _store.Load();
        Company = string.IsNullOrWhiteSpace(profile?.Company) ? "Company not set" : profile.Company;
        Email = string.IsNullOrWhiteSpace(profile?.Email) ? "Not signed in" : profile.Email;
        Plan = string.IsNullOrWhiteSpace(profile?.Plan) ? "Plan: startup" : $"Plan: {profile.Plan}";
        ProductsSummary = CustomerEntitlements.SummarizeProducts(profile?.EnabledProducts);
        CommsSummary = CustomerEntitlements.SummarizeComms(profile?.EnabledComms);
        ApiHostLabel = $"API: {_hostResolver.ApiOrigin}";

        if (_store.IsSignedIn)
            await _api.SyncSessionAsync().ConfigureAwait(false);

        profile = _store.Load();
        if (profile is not null)
        {
            ProductsSummary = CustomerEntitlements.SummarizeProducts(profile.EnabledProducts);
            CommsSummary = CustomerEntitlements.SummarizeComms(profile.EnabledComms);
        }

        BiometricAvailable = await _biometrics.IsAvailableAsync().ConfigureAwait(false);
        BiometricLabel = DeviceInfo.Current.Platform == DevicePlatform.iOS
            ? "Unlock with Face ID"
            : "Unlock with biometrics";

        _suppressBiometricCallback = true;
        BiometricUnlockEnabled = _biometrics.IsEnabled;
        _suppressBiometricCallback = false;
    }

    partial void OnBiometricUnlockEnabledChanged(bool value)
    {
        if (_suppressBiometricCallback)
            return;

        _ = ApplyBiometricPreferenceAsync(value);
    }

    private async Task ApplyBiometricPreferenceAsync(bool enabled)
    {
        if (enabled)
        {
            var authenticated = await _biometrics.AuthenticateAsync(
                "Confirm your identity to enable biometric unlock.").ConfigureAwait(false);
            if (!authenticated)
            {
                _suppressBiometricCallback = true;
                BiometricUnlockEnabled = false;
                _suppressBiometricCallback = false;
                return;
            }
        }

        await _biometrics.SetEnabledAsync(enabled).ConfigureAwait(false);
        _haptics.Click();
    }

    [RelayCommand]
    private Task OpenBillingAsync()
    {
        _haptics.Click();
        return Launcher.OpenAsync(new Uri($"{_config.WebsiteUrl.TrimEnd('/')}/portal/billing"));
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        _haptics.Click();
        await _api.SignOutAsync().ConfigureAwait(false);
        await _navigation.GoToWelcomeAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private Task ReturnToWelcomeAsync()
    {
        _haptics.Click();
        return _navigation.GoToWelcomeAsync();
    }
}
