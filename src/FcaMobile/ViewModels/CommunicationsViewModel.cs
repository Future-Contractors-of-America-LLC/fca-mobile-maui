using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class CommunicationsViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;
    private readonly IHapticFeedbackService _haptics;

    public ObservableCollection<string> Channels { get; } = new();

    public CommunicationsViewModel(
        FcaApiClient api,
        CustomerStore store,
        IConnectivityMonitor connectivity,
        IHapticFeedbackService haptics)
        : base(connectivity)
    {
        _api = api;
        _store = store;
        _haptics = haptics;
    }

    public ObservableCollection<PortalMessage> Messages { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string subject = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string message = string.Empty;

    [ObservableProperty]
    private string selectedChannel = "email";

    [RelayCommand]
    private Task InitializeAsync()
    {
        ApplyEnabledChannels();
        return LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        ApplyEnabledChannels();
        await LoadAsync().ConfigureAwait(false);
        IsRefreshing = false;
    }

    private bool CanSend() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(Subject)
        && !string.IsNullOrWhiteSpace(Message)
        && Channels.Contains(SelectedChannel);

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

    private void ApplyEnabledChannels()
    {
        var profile = _store.Load();
        var enabled = CustomerEntitlements.GetEnabledCommsChannels(profile?.EnabledComms);

        Channels.Clear();
        foreach (var channel in enabled)
            Channels.Add(channel);

        if (Channels.Count == 0)
            Channels.Add("email");

        if (!Channels.Contains(SelectedChannel))
            SelectedChannel = Channels[0];
    }

    partial void OnIsBusyChanged(bool value) => SendCommand.NotifyCanExecuteChanged();
}
