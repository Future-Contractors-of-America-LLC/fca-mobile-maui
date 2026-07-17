using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class AccountPage : ContentPage
{
    private readonly CustomerStore _store;
    private readonly FcaConfig _config;
    private readonly FcaApiClient _api;

    public AccountPage(CustomerStore store, FcaConfig config, FcaApiClient api)
    {
        _store = store;
        _config = config;
        _api = api;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        RenderProfile(_store.Load());

        try
        {
            var profile = await _api.GetCustomerProfileAsync();
            if (profile is not null)
            {
                _store.Save(profile);
                RenderProfile(profile);
            }
        }
        catch
        {
            // Keep the cached profile visible while offline.
        }
    }

    private void RenderProfile(CustomerProfile? profile)
    {
        NameLabel.Text = string.IsNullOrWhiteSpace(profile?.Name) ? "Name not set" : profile.Name;
        CompanyLabel.Text = string.IsNullOrWhiteSpace(profile?.Company) ? "Company not set" : profile.Company;
        EmailLabel.Text = string.IsNullOrWhiteSpace(profile?.Email) ? "Not signed in" : profile.Email;
        PlanLabel.Text = string.IsNullOrWhiteSpace(profile?.Plan) ? "Plan: startup" : $"Plan: {profile.Plan}";
    }

    async void OnMicrosoftSignInClicked(object sender, EventArgs e) =>
        await Launcher.OpenAsync(new Uri($"{_config.WebsiteUrl.TrimEnd('/')}/login"));

    async void OnBillingClicked(object sender, EventArgs e) =>
        await Launcher.OpenAsync(new Uri($"{_config.WebsiteUrl.TrimEnd('/')}/portal/billing"));

    async void OnProfileClicked(object sender, EventArgs e) =>
        await Launcher.OpenAsync(new Uri(_config.BuildPortalHandoffUrl("/portal/profile")));

    async void OnSignOutClicked(object sender, EventArgs e)
    {
        await _api.SignOutAsync();
        _store.Clear();
        await Shell.Current.GoToAsync("//welcome");
    }

    async void OnWelcomeClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//welcome");
}
