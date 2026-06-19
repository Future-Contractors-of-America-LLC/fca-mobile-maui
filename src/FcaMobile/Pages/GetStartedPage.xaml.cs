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
        StatusLabel.IsVisible = false;
        var profile = new CustomerProfile
        {
            Plan = PlanPicker.SelectedItem?.ToString() ?? "startup",
            Company = CompanyEntry.Text?.Trim() ?? "",
            Name = NameEntry.Text?.Trim() ?? "",
            Email = EmailEntry.Text?.Trim() ?? "",
            Password = PasswordEntry.Text ?? "",
        };

        if (string.IsNullOrWhiteSpace(profile.Company) || string.IsNullOrWhiteSpace(profile.Email))
        {
            StatusLabel.Text = "Company name and work email are required.";
            StatusLabel.IsVisible = true;
            return;
        }

        var button = sender as Button;
        if (button is not null)
            button.IsEnabled = false;

        try
        {
            try
            {
                await _api.SubmitLeadIntakeAsync(profile);
            }
            catch (Exception)
            {
                // Intake submission is best-effort; we still save the profile
                // locally so the customer can continue to checkout.
            }

            _store.Save(profile);

            var checkout = profile.Plan == "pilot"
                ? _config.PilotCheckoutUrl
                : string.IsNullOrWhiteSpace(_config.StartupCheckoutUrl) ? _config.WebsiteUrl + "/checkout" : _config.StartupCheckoutUrl;

            await DisplayAlert("Workspace requested", "Your company profile is saved. Complete checkout to activate your plan.", "Continue");

            try
            {
                if (Uri.TryCreate(checkout, UriKind.Absolute, out var checkoutUri))
                    await Launcher.OpenAsync(checkoutUri);
            }
            catch (Exception)
            {
                // A missing browser shouldn't block entry into the workspace.
            }

            await Shell.Current.GoToAsync("//main/command");
        }
        finally
        {
            if (button is not null)
                button.IsEnabled = true;
        }
    }
}
