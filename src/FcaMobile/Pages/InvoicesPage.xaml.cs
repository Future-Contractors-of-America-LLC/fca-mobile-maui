using Fca.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Pages;

public partial class InvoicesPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly ILogger<InvoicesPage> _logger;

    public InvoicesPage(FcaApiClient api, ILogger<InvoicesPage> logger)
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
            InvoiceList.ItemsSource = await _api.GetInvoicesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoices LoadAsync failed");
            await DisplayAlert("Connection error", "Unable to load invoices. Pull down to retry.", "OK");
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
