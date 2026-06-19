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
        finally
        {
            RefreshHost.IsRefreshing = false;
        }
    }

    async void OnRefreshing(object sender, EventArgs e) => await LoadAsync();
}
