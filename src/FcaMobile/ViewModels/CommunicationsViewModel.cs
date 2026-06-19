using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class CommunicationsViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;
    private readonly IHapticFeedbackService _haptics;

    public IReadOnlyList<string> Channels { get; } = ["email", "sms", "portal"];

    public CommunicationsViewModel(
        FcaApiClient api,
        IConnectivityMonitor connectivity,
        IHapticFeedbackService haptics)
        : base(connectivity)
    {
        _api = api;
        _haptics = haptics;
        SelectedChannel = Channels[2];
    }

    public ObservableCollection<PortalMessage> Messages { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string subject = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string message = string.Empty;

    [ObservableProperty]
    private string selectedChannel = "portal";

    [RelayCommand]
    private Task InitializeAsync() => LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync().ConfigureAwait(false);
        IsRefreshing = false;
    }

    private bool CanSend() =>
        !IsBusy && !string.IsNullOrWhiteSpace(Subject) && !string.IsNullOrWhiteSpace(Message);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _api.SendMessageAsync(Subject.Trim(), Message.Trim(), SelectedChannel)
                .ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                SetError(result.ErrorMessage ?? "Unable to send your message.");
                return;
            }

            _haptics.Success();
            Subject = string.Empty;
            Message = string.Empty;
            await LoadAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private Task LoadAsync() => ExecuteAsync(async () =>
    {
        var result = await _api.GetMessagesAsync().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            SetError(result.ErrorMessage ?? "Unable to load messages.");
            return;
        }

        Messages.Clear();
        foreach (var item in result.Value!)
            Messages.Add(item);
    });

    partial void OnIsBusyChanged(bool value) => SendCommand.NotifyCanExecuteChanged();
}
