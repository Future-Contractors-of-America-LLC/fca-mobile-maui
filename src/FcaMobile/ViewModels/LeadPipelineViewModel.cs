using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class LeadPipelineViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;

    public LeadPipelineViewModel(FcaApiClient api, IConnectivityMonitor connectivity)
        : base(connectivity) =>
        _api = api;

    public ObservableCollection<BidRecord> Leads { get; } = new();

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
        var result = await _api.GetLeadsAsync().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            SetError(result.ErrorMessage ?? "Unable to load leads.");
            return;
        }

        Leads.Clear();
        foreach (var lead in result.Value!)
            Leads.Add(lead);
    });
}
