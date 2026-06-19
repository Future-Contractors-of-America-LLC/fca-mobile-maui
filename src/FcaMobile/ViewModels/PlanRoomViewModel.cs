using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class PlanRoomViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;

    public PlanRoomViewModel(FcaApiClient api, IConnectivityMonitor connectivity)
        : base(connectivity) =>
        _api = api;

    public ObservableCollection<FileRecord> Documents { get; } = new();

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
        var result = await _api.GetDocumentsAsync().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            SetError(result.ErrorMessage ?? "Unable to load documents.");
            return;
        }

        Documents.Clear();
        foreach (var document in result.Value!)
            Documents.Add(document);
    });
}
