using Fca.Mobile.Services;
using Fca.Mobile.Utilities;

namespace Fca.Mobile.Pages;

public partial class PlanRoomPage : ContentPage
{
    private readonly FcaApiClient _api;

    public PlanRoomPage(FcaApiClient api)
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
            var result = await _api.GetDocumentsAsync().ConfigureAwait(false);
            if (result.IsSuccess)
                DocList.ItemsSource = result.Value;
            else
            {
                ErrorLabel.Text = result.ErrorMessage ?? "Unable to load documents.";
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
