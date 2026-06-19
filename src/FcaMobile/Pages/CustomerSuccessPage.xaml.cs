using Fca.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Pages;

public partial class CustomerSuccessPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly ILogger<CustomerSuccessPage> _logger;

    public CustomerSuccessPage(FcaApiClient api, ILogger<CustomerSuccessPage> logger)
    {
        _api = api;
        _logger = logger;
        InitializeComponent();
        PriorityPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        SetLoading(true);
        try
        {
            CaseList.ItemsSource = await _api.GetSupportCasesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CustomerSuccess LoadAsync failed");
            await DisplayAlert("Connection error", "Unable to load support cases. Pull down to retry.", "OK");
        }
        finally
        {
            SetLoading(false);
        }
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
        try
        {
            var ok = await _api.CreateSupportCaseAsync(subject, priority, detail);
            if (!ok)
            {
                await DisplayAlert("Submission failed", "Your support case could not be submitted. Please try again.", "OK");
                return;
            }

            SubjectEntry.Text = "";
            DetailEditor.Text = "";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateSupportCase failed");
            await DisplayAlert("Submission failed", "A network error occurred. Please try again.", "OK");
        }
        finally
        {
            CreateButton.IsEnabled = true;
        }
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync();
        RefreshHost.IsRefreshing = false;
    }

    void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
    }
}
