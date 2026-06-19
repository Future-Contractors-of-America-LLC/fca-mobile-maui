using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class TrainingViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;

    public TrainingViewModel(FcaApiClient api, IConnectivityMonitor connectivity)
        : base(connectivity) =>
        _api = api;

    public ObservableCollection<AcademyProgram> Programs { get; } = new();

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
