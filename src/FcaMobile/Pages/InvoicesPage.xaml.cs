using Fca.Mobile.Services;
using Fca.Mobile.Utilities;

namespace Fca.Mobile.Pages;

public partial class InvoicesPage : ContentPage
{
    private readonly FcaApiClient _api;

    public InvoicesPage(FcaApiClient api)
    {
        _api = api;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PageAsync.SafeOnAppearingAsync(this, LoadAsync);
    }

    async Task LoadAsync()
    {
        await PageAsync.RunWithLoadingAsync(LoadingIndicator, ErrorLabel, async () =>
        {
            var result = await _api.GetInvoicesAsync().ConfigureAwait(false);
            if (result.IsSuccess)
                InvoiceList.ItemsSource = result.Value;
            else
            {
                ErrorLabel.Text = result.ErrorMessage ?? "Unable to load invoices.";
                ErrorLabel.IsVisible = true;
            }
        });
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync().ConfigureAwait(false);
        RefreshHost.IsRefreshing = false;
    }
}
