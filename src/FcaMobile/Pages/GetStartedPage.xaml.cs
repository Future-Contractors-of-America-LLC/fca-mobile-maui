using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class GetStartedPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;
    private readonly FcaConfig _config;

    public GetStartedPage(FcaApiClient api, CustomerStore store, FcaConfig config)
    {
        _api = api;
        _store = store;
        _config = config;
        InitializeComponent();
        PlanPicker.SelectedIndex = 0;
    }

    async void OnSubmitClicked(object sender, EventArgs e)
    {
        if (!CreateWorkspaceButton.IsEnabled)
            return;

        StatusLabel.IsVisible = false;
        var profile = new CustomerProfile
        {
            Plan = PlanPicker.SelectedItem?.ToString() ?? "startup",
            Company = CompanyEntry.Text?.Trim() ?? "",
            Name = NameEntry.Text?.Trim() ?? "",
            Email = EmailEntry.Text?.Trim() ?? "",
        };

        if (string.IsNullOrWhiteSpace(profile.Company) || string.IsNullOrWhiteSpace(profile.Email))
        {
            StatusLabel.Text = "Company name and work email are required.";
            StatusLabel.IsVisible = true;
            return;
        }

        SetSubmitting(true);
        try
        {
            var submitted = await _api.SubmitLeadIntakeAsync(profile);
            if (!submitted)
            {
                StatusLabel.Text = "We could not submit your workspace request. Please try again.";
                StatusLabel.IsVisible = true;
                return;
            }

            _store.Save(profile);
        }
        catch (Exception)
        {
            StatusLabel.Text = "Workspace setup is temporarily unavailable. Check your connection and try again.";
            StatusLabel.IsVisible = true;
            return;
        }
        finally
        {
            SetSubmitting(false);
        }

        var checkout = profile.Plan == "pilot"
            ? _config.PilotCheckoutUrl
            : string.IsNullOrWhiteSpace(_config.StartupCheckoutUrl) ? _config.WebsiteUrl + "/checkout" : _config.StartupCheckoutUrl;

        await DisplayAlert("Workspace requested", "Your company profile is saved. Complete checkout to activate your plan, then sign in when your workspace is active.", "Continue");
        await Launcher.OpenAsync(new Uri(checkout));
        await Shell.Current.GoToAsync("//welcome");
    }

    private void SetSubmitting(bool isSubmitting)
    {
        CreateWorkspaceButton.IsEnabled = !isSubmitting;
        BusyIndicator.IsVisible = isSubmitting;
        BusyIndicator.IsRunning = isSubmitting;
    }
}
