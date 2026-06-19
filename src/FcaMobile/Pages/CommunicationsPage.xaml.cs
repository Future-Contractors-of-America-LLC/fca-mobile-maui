using Fca.Mobile.Services;
using Fca.Mobile.Utilities;

namespace Fca.Mobile.Pages;

public partial class CommunicationsPage : ContentPage
{
    private readonly FcaApiClient _api;

    public CommunicationsPage(FcaApiClient api)
    {
        _api = api;
        InitializeComponent();
        ChannelPicker.SelectedIndex = 0;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PageAsync.SafeOnAppearingAsync(this, LoadAsync);
    }

    async Task LoadAsync()
    {
        await PageAsync.RunWithLoadingAsync(LoadingIndicator, ErrorLabel, async () =>
        {
            var result = await _api.GetMessagesAsync().ConfigureAwait(false);
            if (result.IsSuccess)
                MessageList.ItemsSource = result.Value;
            else
            {
                ErrorLabel.Text = result.ErrorMessage ?? "Unable to load messages.";
                ErrorLabel.IsVisible = true;
            }
        });
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
        await PageAsync.RunWithLoadingAsync(LoadingIndicator, ErrorLabel, async () =>
        {
            var result = await _api.SendMessageAsync(subject, message, channel).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                ErrorLabel.Text = result.ErrorMessage ?? "Unable to send your message.";
                ErrorLabel.IsVisible = true;
                return;
            }

            SubjectEntry.Text = "";
            MessageEditor.Text = "";
            await LoadAsync().ConfigureAwait(false);
        });
        SendButton.IsEnabled = true;
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync().ConfigureAwait(false);
        RefreshHost.IsRefreshing = false;
    }
}
