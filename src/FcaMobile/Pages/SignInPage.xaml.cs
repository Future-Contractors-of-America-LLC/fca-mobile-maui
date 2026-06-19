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
        if (!SignInButton.IsEnabled)
            return;

        StatusLabel.IsVisible = false;
        var email = EmailEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            StatusLabel.Text = "Enter your work email and password.";
            StatusLabel.IsVisible = true;
            return;
        }

        SetSubmitting(true);
        try
        {
            var result = await _api.SignInAsync(email, password);
            if (!result.IsAuthenticated)
            {
                StatusLabel.Text = "We could not verify those credentials. Check your email and password.";
                StatusLabel.IsVisible = true;
                return;
            }

            var profile = _store.Load() ?? new CustomerProfile();
            await _store.CompleteSignInAsync(email, result.AuthToken, profile);
            PasswordEntry.Text = string.Empty;
            await Shell.Current.GoToAsync("//main/command");
        }
        catch (Exception)
        {
            StatusLabel.Text = "Sign in is temporarily unavailable. Check your connection and try again.";
            StatusLabel.IsVisible = true;
        }
        finally
        {
            SetSubmitting(false);
        }
    }

    async void OnGetStartedClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("getstarted");

    private void SetSubmitting(bool isSubmitting)
    {
        SignInButton.IsEnabled = !isSubmitting;
        GetStartedButton.IsEnabled = !isSubmitting;
        BusyIndicator.IsVisible = isSubmitting;
        BusyIndicator.IsRunning = isSubmitting;
    }
}
