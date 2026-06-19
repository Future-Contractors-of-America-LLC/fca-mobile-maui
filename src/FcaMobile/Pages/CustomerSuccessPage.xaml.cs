using Fca.Mobile.Services;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync() => CaseList.ItemsSource = await _api.GetSupportCasesAsync();

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

        await _api.CreateSupportCaseAsync(subject, priority, detail);
        SubjectEntry.Text = "";
        DetailEditor.Text = "";
        await LoadAsync();
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync();
        RefreshHost.IsRefreshing = false;
    }
}
