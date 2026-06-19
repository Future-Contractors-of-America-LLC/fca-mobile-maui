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
            ShowStatus("Enter your work email and password.");
            return;
        }

        if (!Validation.IsValidEmail(email))
        {
            ShowStatus("Enter a valid work email address.");
            return;
        }

        var button = sender as Button;
        if (button is not null)
            button.IsEnabled = false;

        try
        {
            var ok = await _api.SignInAsync(email, password);
            if (!ok)
            {
                ShowStatus("We could not verify those credentials. Check your email and password, then try again.");
                return;
            }

            _store.Save(new CustomerProfile { Email = email });
            await Shell.Current.GoToAsync("//main/command");
        }
        finally
        {
            if (button is not null)
                button.IsEnabled = true;
        }
    }

    void ShowStatus(string message)
    {
        StatusLabel.Text = message;
        StatusLabel.IsVisible = true;
    }

    async void OnGetStartedClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("getstarted");
}
