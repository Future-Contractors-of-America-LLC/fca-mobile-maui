using Fca.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Pages;

public partial class PlanRoomPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly ILogger<PlanRoomPage> _logger;

    public PlanRoomPage(FcaApiClient api, ILogger<PlanRoomPage> logger)
    {
        _api = api;
        _logger = logger;
        InitializeComponent();
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
            DocList.ItemsSource = await _api.GetDocumentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PlanRoom LoadAsync failed");
            await DisplayAlert("Connection error", "Unable to load plan room documents. Pull down to retry.", "OK");
        }
        finally
        {
            SetLoading(false);
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
