using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class GetStartedViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;
    private readonly FcaConfig _config;
    private readonly IShellNavigation _navigation;
    private readonly IHapticFeedbackService _haptics;

    public IReadOnlyList<string> Plans { get; } = PlanCatalog.IntakePlans;

    public GetStartedViewModel(
        FcaApiClient api,
        CustomerStore store,
        FcaConfig config,
        IConnectivityMonitor connectivity,
        IShellNavigation navigation,
        IHapticFeedbackService haptics)
        : base(connectivity)
    {
        _api = api;
        _store = store;
        _config = config;
        _navigation = navigation;
        _haptics = haptics;
        SelectedPlan = Plans[0];
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string selectedPlan = "startup";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string company = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    private bool CanSubmit() =>
        !IsBusy && !string.IsNullOrWhiteSpace(Company) && !string.IsNullOrWhiteSpace(Email);

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        await ExecuteAsync(async () =>
        {
            var profile = new CustomerProfile
            {
                Plan = SelectedPlan,
                Company = Company.Trim(),
                Name = Name.Trim(),
                Email = Email.Trim(),
                Password = Password,
            };

            var result = await _api.SubmitLeadIntakeAsync(profile).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                SetError(result.ErrorMessage ?? "Unable to create your workspace.");
                return;
            }

            await _store.SaveAsync(profile, profile.Password).ConfigureAwait(false);
            _haptics.Success();

            var checkout = PlanCatalog.CheckoutUrl(_config, profile.Plan, profile.Email);

            if (profile.Plan == "enterprise")
            {
                await Shell.Current.DisplayAlert(
                    "Workspace requested",
                    "Your company profile is saved. Our team will follow up for enterprise onboarding.",
                    "Continue").ConfigureAwait(false);
                await Launcher.OpenAsync(new Uri(checkout)).ConfigureAwait(false);
                await _navigation.GoToWelcomeAsync().ConfigureAwait(false);
                return;
            }

            await Shell.Current.DisplayAlert(
                "Workspace requested",
                "Your company profile is saved. Complete checkout to activate your plan.",
                "Continue").ConfigureAwait(false);

            await Launcher.OpenAsync(new Uri(checkout)).ConfigureAwait(false);
            await _navigation.GoToMainAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    partial void OnIsBusyChanged(bool value) => SubmitCommand.NotifyCanExecuteChanged();
}
