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

    async Task LoadAsync()
    {
        try
        {
            DocList.ItemsSource = await _api.GetDocumentsAsync();
        }
        catch
        {
            await this.ShowLoadErrorAsync("plan room documents");
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
