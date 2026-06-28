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

    async void OnSignInClicked(object sender, EventArgs e) => await SignInAsync();

    private async Task SignInAsync()
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

        IsBusy = true;
        SignInButton.IsEnabled = false;

        try
        {
            var result = await _api.SignInAsync(email, password);
            if (!result.IsSuccessful || string.IsNullOrWhiteSpace(result.AccessToken))
            {
                ShowStatus(result.ErrorMessage ?? "We could not verify those credentials. Check your email and password.");
                return;
            }

            _store.Save(new CustomerProfile { Email = email });
            await _store.SaveAccessTokenAsync(result.AccessToken);
            PasswordEntry.Text = "";
            await Shell.Current.GoToAsync("//main/command");
        }
        catch
        {
            ShowStatus("Sign in is temporarily unavailable. Check your connection and try again.");
        }
        finally
        {
            SignInButton.IsEnabled = true;
            IsBusy = false;
        }
    }

    async void OnGetStartedClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("getstarted");

    private void ShowStatus(string message)
    {
        StatusLabel.Text = message;
        StatusLabel.IsVisible = true;
    }
}
