using Fca.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Pages;

public partial class TrainingPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly ILogger<TrainingPage> _logger;

    public TrainingPage(FcaApiClient api, ILogger<TrainingPage> logger)
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
            var snapshot = await _api.GetTrainingAsync();
            ProgramList.ItemsSource = snapshot?.Catalog?.Programs ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Training LoadAsync failed");
            await DisplayAlert("Connection error", "Unable to load training catalog. Pull down to retry.", "OK");
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
