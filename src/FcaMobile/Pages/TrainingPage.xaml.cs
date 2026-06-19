using Fca.Mobile.Services;
using Fca.Mobile.Utilities;

namespace Fca.Mobile.Pages;

public partial class TrainingPage : ContentPage
{
    private readonly FcaApiClient _api;

    public TrainingPage(FcaApiClient api)
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
            var result = await _api.GetTrainingAsync().ConfigureAwait(false);
            if (result.IsSuccess)
                ProgramList.ItemsSource = result.Value?.Catalog?.Programs ?? new List<Models.AcademyProgram>();
            else
            {
                ErrorLabel.Text = result.ErrorMessage ?? "Unable to load training programs.";
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
