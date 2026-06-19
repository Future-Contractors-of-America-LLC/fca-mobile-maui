using Fca.Mobile.Models;
using Fca.Mobile.Services;
using Fca.Mobile.Utilities;

namespace Fca.Mobile.Pages;

public partial class SignInPage : ContentPage
{
    private readonly FcaApiClient _api;

    public SignInPage(FcaApiClient api)
    {
        _api = api;
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

        SignInButton.IsEnabled = false;
        await PageAsync.RunWithLoadingAsync(LoadingIndicator, StatusLabel, async () =>
        {
            var result = await _api.SignInAsync(email, password).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                StatusLabel.Text = result.ErrorMessage ?? "Sign in failed.";
                StatusLabel.IsVisible = true;
                return;
            }

            await Shell.Current.GoToAsync("//main/command").ConfigureAwait(false);
        });
        SignInButton.IsEnabled = true;
    }

    async void OnGetStartedClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("getstarted");
}
