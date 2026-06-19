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
        await LoadAsync(showSpinner: true);
    }

    async Task LoadAsync(bool showSpinner)
    {
        ErrorView.IsVisible = false;
        if (showSpinner)
            Busy.IsVisible = Busy.IsRunning = true;

        try
        {
            MessageList.ItemsSource = await _api.GetMessagesAsync();
        }
        catch (Exception)
        {
            ErrorView.IsVisible = true;
        }
        finally
        {
            Busy.IsVisible = Busy.IsRunning = false;
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
            await _api.SendMessageAsync(subject, message, channel);
            SubjectEntry.Text = "";
            MessageEditor.Text = "";
            await LoadAsync(showSpinner: false);
        }
        catch (Exception)
        {
            await DisplayAlert("Message not sent", "We couldn't send your message. Check your connection and try again.", "OK");
        }
        finally
        {
            if (button is not null)
                button.IsEnabled = true;
        }
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync(showSpinner: false);
        RefreshHost.IsRefreshing = false;
    }

    async void OnRetryClicked(object sender, EventArgs e) => await LoadAsync(showSpinner: true);
}
