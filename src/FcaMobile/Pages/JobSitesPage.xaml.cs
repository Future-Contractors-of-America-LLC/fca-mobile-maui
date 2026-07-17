using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class JobSitesPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly FcaConfig _config;

    public JobSitesPage(FcaApiClient api, FcaConfig config)
    {
        _api = api;
        _config = config;
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
        catch
        {
            await this.ShowLoadErrorAsync("jobs");
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

    async void OnJobSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        if (e.CurrentSelection.FirstOrDefault() is not ProjectRecord project ||
            string.IsNullOrWhiteSpace(project.ProjectId))
            return;

        var projectId = Uri.EscapeDataString(project.ProjectId);
        var url = _config.BuildPortalHandoffUrl($"/portal/projects/{projectId}");

        try
        {
            await Launcher.Default.OpenAsync(url);
        }
        catch
        {
            await DisplayAlert("Project unavailable", "Could not open this project in FCA Contractor Command.", "OK");
        }
    }
}
