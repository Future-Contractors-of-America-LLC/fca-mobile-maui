using Fca.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Pages;

public partial class CommunicationsPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly ILogger<CommunicationsPage> _logger;

    public CommunicationsPage(FcaApiClient api, ILogger<CommunicationsPage> logger)
    {
        _api = api;
        _logger = logger;
        InitializeComponent();
        ChannelPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        SetLoading(true);
        try
        {
            MessageList.ItemsSource = await _api.GetMessagesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Communications LoadAsync failed");
            await DisplayAlert("Connection error", "Unable to load messages. Pull down to retry.", "OK");
        }
        finally
        {
            SetLoading(false);
        }
    }

    async void OnSendClicked(object sender, EventArgs e)
    {
        var subject = SubjectEntry.Text?.Trim() ?? "";
        var message = MessageEditor.Text?.Trim() ?? "";
        var channel = ChannelPicker.SelectedItem?.ToString() ?? "portal";

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            await DisplayAlert("Message required", "Add a subject and message before sending.", "OK");
            return;
        }

        SendButton.IsEnabled = false;
        try
        {
            var ok = await _api.SendMessageAsync(subject, message, channel);
            if (!ok)
            {
                await DisplayAlert("Send failed", "Your message could not be delivered. Please try again.", "OK");
                return;
            }

            SubjectEntry.Text = "";
            MessageEditor.Text = "";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessage failed");
            await DisplayAlert("Send failed", "A network error occurred. Please try again.", "OK");
        }
        finally
        {
            SendButton.IsEnabled = true;
        }
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync();
        RefreshHost.IsRefreshing = false;
    }

    void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
    }
}
