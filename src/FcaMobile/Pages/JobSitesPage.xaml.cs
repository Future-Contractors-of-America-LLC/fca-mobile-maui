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

    async Task LoadAsync()
    {
        try
        {
            JobList.ItemsSource = await _api.GetJobsAsync();
        }
        finally
        {
            RefreshHost.IsRefreshing = false;
        }
    }

    async void OnRefreshing(object sender, EventArgs e) => await LoadAsync();
}
