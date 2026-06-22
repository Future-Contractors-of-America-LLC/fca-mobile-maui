using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class JobSitesViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;

    public JobSitesViewModel(FcaApiClient api, IConnectivityMonitor connectivity)
        : base(connectivity) =>
        _api = api;

    public ObservableCollection<ProjectRecord> Jobs { get; } = new();

    [RelayCommand]
    private Task InitializeAsync() => LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync().ConfigureAwait(false);
        IsRefreshing = false;
    }

    private Task LoadAsync() => ExecuteAsync(async () =>
    {
        var result = await _api.GetJobsAsync().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            SetError(result.ErrorMessage ?? "Unable to load jobs.");
            return;
        }

        Jobs.Clear();
        foreach (var job in result.Value!)
            Jobs.Add(job);
    });
}
