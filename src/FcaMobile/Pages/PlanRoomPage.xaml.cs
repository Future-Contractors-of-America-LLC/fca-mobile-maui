using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class PlanRoomPage : ContentPage
{
    private readonly FcaApiClient _api;

    public PlanRoomPage(FcaApiClient api)
    {
        _api = api;
        InitializeComponent();
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
            DocList.ItemsSource = await _api.GetDocumentsAsync();
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

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync(showSpinner: false);
        RefreshHost.IsRefreshing = false;
    }

    async void OnRetryClicked(object sender, EventArgs e) => await LoadAsync(showSpinner: true);
}
