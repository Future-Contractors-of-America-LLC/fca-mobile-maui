using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class CommandCenterViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;
    private readonly IShellNavigation _navigation;
    private readonly IHapticFeedbackService _haptics;

    public CommandCenterViewModel(
        FcaApiClient api,
        CustomerStore store,
        IConnectivityMonitor connectivity,
        IShellNavigation navigation,
        IHapticFeedbackService haptics)
        : base(connectivity)
    {
        _api = api;
        _store = store;
        _navigation = navigation;
        _haptics = haptics;
    }

    [ObservableProperty]
    private string greeting = "Your Command Center";

    [ObservableProperty]
    private string leadCount = "0";

    [ObservableProperty]
    private string jobCount = "0";

    [ObservableProperty]
    private string docCount = "0";

    [ObservableProperty]
    private string trainingCount = "0";

    [RelayCommand]
    private Task InitializeAsync() => LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync().ConfigureAwait(false);
        IsRefreshing = false;
    }

    [RelayCommand]
    private Task OpenLeadsAsync()
    {
        _haptics.Click();
        return _navigation.GoToAsync("//main/leads");
    }

    [RelayCommand]
    private Task OpenPlanRoomAsync()
    {
        _haptics.Click();
        return _navigation.GoToAsync("planroom");
    }

    [RelayCommand]
    private Task OpenInvoicesAsync()
    {
        _haptics.Click();
        return _navigation.GoToAsync("invoices");
    }

    [RelayCommand]
    private Task OpenCommunicationsAsync()
    {
        _haptics.Click();
        return _navigation.GoToAsync("communications");
    }

    [RelayCommand]
    private Task OpenSupportAsync()
    {
        _haptics.Click();
        return _navigation.GoToAsync("support");
    }

    private Task LoadAsync() => ExecuteAsync(async () =>
    {
        var profile = _store.Load();
        Greeting = profile?.Company is { Length: > 0 } company
            ? $"{company} Command Center"
            : "Your Command Center";

        var leads = await _api.GetLeadsAsync().ConfigureAwait(false);
        var jobs = await _api.GetJobsAsync().ConfigureAwait(false);
        var docs = await _api.GetDocumentsAsync().ConfigureAwait(false);

        var lmsEnabled = CustomerEntitlements.IsProductEnabled(profile?.EnabledProducts, "lms");
        ApiResult<AcademySnapshot>? training = null;
        if (lmsEnabled)
            training = await _api.GetTrainingAsync().ConfigureAwait(false);

        if (!leads.IsSuccess || !jobs.IsSuccess || !docs.IsSuccess || (lmsEnabled && training is { IsSuccess: false }))
        {
            SetError(leads.ErrorMessage
                ?? jobs.ErrorMessage
                ?? docs.ErrorMessage
                ?? training?.ErrorMessage
                ?? "Unable to load your command center.");
            return;
        }

        LeadCount = leads.Value!.Count.ToString();
        JobCount = jobs.Value!.Count.ToString();
        DocCount = docs.Value!.Count.ToString();
        TrainingCount = lmsEnabled
            ? (training!.Value?.Catalog?.Programs?.Count ?? 0).ToString()
            : "Pending";
    });
}
