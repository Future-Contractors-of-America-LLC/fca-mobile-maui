using Fca.Mobile.Services;
using Fca.Mobile.Utilities;

namespace Fca.Mobile.Pages;

public partial class JobSitesPage : ContentPage
{
    private readonly FcaApiClient _api;

    public JobSitesPage(FcaApiClient api)
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
            var result = await _api.GetJobsAsync().ConfigureAwait(false);
            if (result.IsSuccess)
                JobList.ItemsSource = result.Value;
            else
            {
                ErrorLabel.Text = result.ErrorMessage ?? "Unable to load jobs.";
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
