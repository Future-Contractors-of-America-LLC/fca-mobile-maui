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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var profile = _store.Load();
        CompanyLabel.Text = string.IsNullOrWhiteSpace(profile?.Company) ? "Company not set" : profile.Company;
        EmailLabel.Text = string.IsNullOrWhiteSpace(profile?.Email) ? "Not signed in" : profile.Email;
        PlanLabel.Text = string.IsNullOrWhiteSpace(profile?.Plan) ? "Plan: startup" : $"Plan: {profile.Plan}";
    }

    async void OnMicrosoftSignInClicked(object sender, EventArgs e) =>
        await Launcher.OpenAsync(new Uri($"{_config.WebsiteUrl.TrimEnd('/')}/login"));

    async void OnBillingClicked(object sender, EventArgs e) =>
        await Launcher.OpenAsync(new Uri($"{_config.WebsiteUrl.TrimEnd('/')}/portal/billing"));

    async void OnSignOutClicked(object sender, EventArgs e)
    {
        await _api.SignOutAsync();
        _store.Clear();
        await Shell.Current.GoToAsync("//welcome");
    }

    async void OnWelcomeClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//welcome");
}
