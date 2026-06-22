using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class TrainingViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;
    private readonly FcaConfig _config;

    public TrainingViewModel(
        FcaApiClient api,
        CustomerStore store,
        FcaConfig config,
        IConnectivityMonitor connectivity)
        : base(connectivity)
    {
        _api = api;
        _store = store;
        _config = config;
    }

    public ObservableCollection<AcademyProgram> Programs { get; } = new();

    [ObservableProperty]
    private bool isLmsEnabled = true;

    [ObservableProperty]
    private string accessMessage = string.Empty;

    [RelayCommand]
    private Task InitializeAsync() => LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync().ConfigureAwait(false);
        IsRefreshing = false;
    }

    [RelayCommand]
    private Task OpenAcademyOnWebAsync() =>
        Launcher.OpenAsync(new Uri($"{_config.WebsiteUrl.TrimEnd('/')}/academy"));

    private Task LoadAsync() => ExecuteAsync(async () =>
    {
        var profile = _store.Load();
        IsLmsEnabled = CustomerEntitlements.IsProductEnabled(profile?.EnabledProducts, "lms");

        if (!IsLmsEnabled)
        {
            Programs.Clear();
            AccessMessage =
                "Academy / LMS is pending for this plan. Open the web academy to review upgrade options or contact customer success.";
            return;
        }

        AccessMessage = string.Empty;
        var result = await _api.GetTrainingAsync().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            SetError(result.ErrorMessage ?? "Unable to load training programs.");
            return;
        }

        Programs.Clear();
        foreach (var program in result.Value?.Catalog?.Programs ?? [])
            Programs.Add(program);
    });
}
