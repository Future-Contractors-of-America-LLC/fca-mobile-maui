using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class PlanRoomPage : ContentPage
{
    private readonly FcaApiClient _api;

    public PlanRoomPage(FcaApiClient api)
    {
        _api = api;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync() => DocList.ItemsSource = await _api.GetDocumentsAsync();

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync();
        RefreshHost.IsRefreshing = false;
    }
}
