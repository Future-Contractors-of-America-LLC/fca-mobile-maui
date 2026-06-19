using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class InvoicesPage : ContentPage
{
    private readonly FcaApiClient _api;

    public InvoicesPage(FcaApiClient api)
    {
        _api = api;
        InitializeComponent();
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
            InvoiceList.ItemsSource = await _api.GetInvoicesAsync();
        }
        catch
        {
            await this.ShowLoadErrorAsync("invoices");
        }
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        try
        {
            await LoadAsync();
        }
        finally
        {
            RefreshHost.IsRefreshing = false;
        }
    }
}
