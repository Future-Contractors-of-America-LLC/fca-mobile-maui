using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class CustomerSuccessViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;
    private readonly IHapticFeedbackService _haptics;

    public IReadOnlyList<string> Priorities { get; } = ["standard", "urgent", "critical"];

    public CustomerSuccessViewModel(
        FcaApiClient api,
        IConnectivityMonitor connectivity,
        IHapticFeedbackService haptics)
        : base(connectivity)
    {
        _api = api;
        _haptics = haptics;
        SelectedPriority = Priorities[0];
    }

    public ObservableCollection<SupportTicket> Cases { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCaseCommand))]
    private string subject = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCaseCommand))]
    private string detail = string.Empty;

    [ObservableProperty]
    private string selectedPriority = "standard";

    [RelayCommand]
    private Task InitializeAsync() => LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync().ConfigureAwait(false);
        IsRefreshing = false;
    }

    private bool CanCreateCase() =>
        !IsBusy && !string.IsNullOrWhiteSpace(Subject) && !string.IsNullOrWhiteSpace(Detail);

    [RelayCommand(CanExecute = nameof(CanCreateCase))]
    private async Task CreateCaseAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _api.CreateSupportCaseAsync(Subject.Trim(), SelectedPriority, Detail.Trim())
                .ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                SetError(result.ErrorMessage ?? "Unable to open your support case.");
                return;
            }

            _haptics.Success();
            Subject = string.Empty;
            Detail = string.Empty;
            await LoadAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private Task LoadAsync() => ExecuteAsync(async () =>
    {
        var result = await _api.GetSupportCasesAsync().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            SetError(result.ErrorMessage ?? "Unable to load support cases.");
            return;
        }

        Cases.Clear();
        foreach (var supportCase in result.Value!)
            Cases.Add(supportCase);
    });

    partial void OnIsBusyChanged(bool value) => CreateCaseCommand.NotifyCanExecuteChanged();
}
