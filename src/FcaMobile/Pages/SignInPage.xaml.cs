using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class SignInPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;

    public SignInPage(FcaApiClient api, CustomerStore store)
    {
        _api = api;
        _store = store;
        InitializeComponent();
    }

    async void OnSignInClicked(object sender, EventArgs e)
    {
        StatusLabel.IsVisible = false;
        var email = EmailEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            StatusLabel.Text = "Enter your work email and password.";
            StatusLabel.IsVisible = true;
            return;
        }

        var ok = await _api.SignInAsync(email, password);
        if (!ok)
        {
            StatusLabel.Text = "We could not verify those credentials. Check your email and password.";
            StatusLabel.IsVisible = true;
            return;
        }

        _store.Save(new CustomerProfile { Email = email, Password = password });
        await Shell.Current.GoToAsync("//main/command");
    }

    async void OnGetStartedClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("getstarted");
}
