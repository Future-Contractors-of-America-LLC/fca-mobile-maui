using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class LeadPipelinePage : ContentPage
{
    private readonly FcaApiClient _api;

    public LeadPipelinePage(FcaApiClient api)
    {
        _api = api;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync() => LeadList.ItemsSource = await _api.GetLeadsAsync();

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync();
        RefreshHost.IsRefreshing = false;
    }
}
