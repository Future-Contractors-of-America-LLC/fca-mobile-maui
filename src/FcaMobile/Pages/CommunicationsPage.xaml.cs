using Fca.Mobile.Services;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        try
        {
            MessageList.ItemsSource = await _api.GetMessagesAsync();
        }
        finally
        {
            RefreshHost.IsRefreshing = false;
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

        var button = sender as Button;
        if (button is not null)
            button.IsEnabled = false;

        try
        {
            var sent = await _api.SendMessageAsync(subject, message, channel);
            if (!sent)
            {
                await DisplayAlert("Message not sent", "We could not reach your FCA success team. Check your connection and try again.", "OK");
                return;
            }

            SubjectEntry.Text = "";
            MessageEditor.Text = "";
            await LoadAsync();
        }
        finally
        {
            if (button is not null)
                button.IsEnabled = true;
        }
    }

    async void OnRefreshing(object sender, EventArgs e) => await LoadAsync();
}
