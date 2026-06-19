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

    async Task LoadAsync() => MessageList.ItemsSource = await _api.GetMessagesAsync();

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

        await _api.SendMessageAsync(subject, message, channel);
        SubjectEntry.Text = "";
        MessageEditor.Text = "";
        await LoadAsync();
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync();
        RefreshHost.IsRefreshing = false;
    }
}
