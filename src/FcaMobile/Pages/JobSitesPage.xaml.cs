using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class JobSitesPage : ContentPage
{
    private readonly FcaApiClient _api;

    public JobSitesPage(FcaApiClient api)
    {
        _api = api;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync() => JobList.ItemsSource = await _api.GetJobsAsync();

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync();
        RefreshHost.IsRefreshing = false;
    }
}
