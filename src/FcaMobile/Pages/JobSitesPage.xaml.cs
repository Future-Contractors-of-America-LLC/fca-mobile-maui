using Fca.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Pages;

public partial class JobSitesPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly ILogger<JobSitesPage> _logger;

    public JobSitesPage(FcaApiClient api, ILogger<JobSitesPage> logger)
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
            JobList.ItemsSource = await _api.GetJobsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JobSites LoadAsync failed");
            await DisplayAlert("Connection error", "Unable to load job sites. Pull down to retry.", "OK");
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
