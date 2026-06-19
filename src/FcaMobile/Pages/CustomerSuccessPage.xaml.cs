using Fca.Mobile.Services;
using Fca.Mobile.Utilities;

namespace Fca.Mobile.Pages;

public partial class CustomerSuccessPage : ContentPage
{
    private readonly FcaApiClient _api;

    public CustomerSuccessPage(FcaApiClient api)
    {
        _api = api;
        InitializeComponent();
        PriorityPicker.SelectedIndex = 0;
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
            var result = await _api.GetSupportCasesAsync().ConfigureAwait(false);
            if (result.IsSuccess)
                CaseList.ItemsSource = result.Value;
            else
            {
                ErrorLabel.Text = result.ErrorMessage ?? "Unable to load support cases.";
                ErrorLabel.IsVisible = true;
            }
        });
    }

    async void OnCreateClicked(object sender, EventArgs e)
    {
        var subject = SubjectEntry.Text?.Trim() ?? "";
        var detail = DetailEditor.Text?.Trim() ?? "";
        var priority = PriorityPicker.SelectedItem?.ToString() ?? "standard";
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(detail))
        {
            await DisplayAlert("Case details required", "Add a subject and description for your support case.", "OK");
            return;
        }

        CreateButton.IsEnabled = false;
        await PageAsync.RunWithLoadingAsync(LoadingIndicator, ErrorLabel, async () =>
        {
            var result = await _api.CreateSupportCaseAsync(subject, priority, detail).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                ErrorLabel.Text = result.ErrorMessage ?? "Unable to open your support case.";
                ErrorLabel.IsVisible = true;
                return;
            }

            SubjectEntry.Text = "";
            DetailEditor.Text = "";
            await LoadAsync().ConfigureAwait(false);
        });
        CreateButton.IsEnabled = true;
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync().ConfigureAwait(false);
        RefreshHost.IsRefreshing = false;
    }
}
