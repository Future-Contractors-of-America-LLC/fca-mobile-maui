using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class AccountPage : ContentPage
{
    private readonly CustomerStore _store;
    private readonly FcaConfig _config;

    public AccountPage(CustomerStore store, FcaConfig config)
    {
        _store = store;
        _config = config;
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

    async void OnBillingClicked(object sender, EventArgs e) =>
        await Launcher.OpenAsync(new Uri($"{_config.WebsiteUrl.TrimEnd('/')}/portal/billing"));

    async void OnSignOutClicked(object sender, EventArgs e)
    {
        _store.Clear();
        await Shell.Current.GoToAsync("//welcome");
    }

    async void OnWelcomeClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//welcome");
}
