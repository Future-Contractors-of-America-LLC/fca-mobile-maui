using Fca.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Pages;

public partial class LeadPipelinePage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly ILogger<LeadPipelinePage> _logger;

    public LeadPipelinePage(FcaApiClient api, ILogger<LeadPipelinePage> logger)
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
            LeadList.ItemsSource = await _api.GetLeadsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LeadPipeline LoadAsync failed");
            await DisplayAlert("Connection error", "Unable to load leads. Pull down to retry.", "OK");
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
