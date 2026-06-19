using Fca.Mobile.Models;
using Fca.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Pages;

public partial class GetStartedPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;
    private readonly FcaConfig _config;
    private readonly ILogger<GetStartedPage> _logger;

    public GetStartedPage(FcaApiClient api, CustomerStore store, FcaConfig config, ILogger<GetStartedPage> logger)
    {
        _api = api;
        _store = store;
        _config = config;
        _logger = logger;
        InitializeComponent();
        PlanPicker.SelectedIndex = 0;
    }

    async void OnSubmitClicked(object sender, EventArgs e)
    {
        StatusLabel.IsVisible = false;

        var profile = new CustomerProfile
        {
            Plan = PlanPicker.SelectedItem?.ToString() ?? "startup",
            Company = CompanyEntry.Text?.Trim() ?? "",
            Name = NameEntry.Text?.Trim() ?? "",
            Email = EmailEntry.Text?.Trim() ?? "",
        };
        var password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(profile.Company) || string.IsNullOrWhiteSpace(profile.Email))
        {
            StatusLabel.Text = "Company name and work email are required.";
            StatusLabel.IsVisible = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            StatusLabel.Text = "Please create a password for your account.";
            StatusLabel.IsVisible = true;
            return;
        }

        SetBusy(true);
        try
        {
            await _api.SubmitLeadIntakeAsync(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lead intake submission failed; saving profile locally and proceeding");
            StatusLabel.Text = "Could not reach the server. Your profile is saved and will sync when connected.";
            StatusLabel.IsVisible = true;
        }
        finally
        {
            SetBusy(false);
        }

        _store.Save(profile);
        await _store.SaveCredentialsAsync(profile.Email, password);

        var checkout = profile.Plan == "pilot"
            ? _config.PilotCheckoutUrl
            : string.IsNullOrWhiteSpace(_config.StartupCheckoutUrl)
                ? _config.WebsiteUrl.TrimEnd('/') + "/checkout"
                : _config.StartupCheckoutUrl;

        await DisplayAlert("Workspace requested", "Your company profile is saved. Complete checkout to activate your plan.", "Continue");
        await Launcher.OpenAsync(new Uri(checkout));
        await Shell.Current.GoToAsync("//main/command");
    }

    void SetBusy(bool busy)
    {
        LoadingIndicator.IsRunning = busy;
        LoadingIndicator.IsVisible = busy;
        SubmitButton.IsEnabled = !busy;
        PlanPicker.IsEnabled = !busy;
        CompanyEntry.IsEnabled = !busy;
        NameEntry.IsEnabled = !busy;
        EmailEntry.IsEnabled = !busy;
        PasswordEntry.IsEnabled = !busy;
    }
}
