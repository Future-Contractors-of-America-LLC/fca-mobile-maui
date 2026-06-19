using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class CustomerSuccessPage : ContentPage
{
    private readonly FcaApiClient _api;

    public CustomerSuccessPage(FcaApiClient api)
    {
        _api = api;
        InitializeComponent();
        PriorityPicker.SelectedIndex = 0;
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
            CaseList.ItemsSource = await _api.GetSupportCasesAsync();
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

    async void OnCreateClicked(object sender, EventArgs e)
    {
        var subject = SubjectEntry.Text?.Trim() ?? "";
        var detail = DetailEditor.Text?.Trim() ?? "";
        var priority = PriorityPicker.SelectedItem?.ToString() ?? "standard";
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(detail))
        {
            await DisplayAlert("Case details required", "Add a subject and description for your support case.", "OK");
            return;
        }

        var button = sender as Button;
        if (button is not null)
            button.IsEnabled = false;

        try
        {
            await _api.CreateSupportCaseAsync(subject, priority, detail);
            SubjectEntry.Text = "";
            DetailEditor.Text = "";
            await LoadAsync(showSpinner: false);
        }
        catch (Exception)
        {
            await DisplayAlert("Case not created", "We couldn't open your support case. Check your connection and try again.", "OK");
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
