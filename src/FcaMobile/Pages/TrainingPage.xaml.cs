using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class TrainingPage : ContentPage
{
    private readonly FcaApiClient _api;

    public TrainingPage(FcaApiClient api)
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
            var snapshot = await _api.GetTrainingAsync();
            ProgramList.ItemsSource = snapshot?.Catalog?.Programs ?? new List<Models.AcademyProgram>();
        }
        catch
        {
            await this.ShowLoadErrorAsync("training programs");
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
