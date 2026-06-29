using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class SignInPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;
    private readonly FcaConfig _config;
    private bool _passwordVisible;
    private string? _pendingChallengeId;
    private string? _pendingEmail;

    public SignInPage(FcaApiClient api, CustomerStore store, FcaConfig config)
    {
        _api = api;
        _store = store;
        _config = config;
        InitializeComponent();
    }

    async void OnSignInClicked(object sender, EventArgs e) => await SignInAsync();

    async void OnVerifyClicked(object sender, EventArgs e) => await VerifyAsync();

    void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        PasswordEntry.IsPassword = !_passwordVisible;
        TogglePasswordButton.Text = _passwordVisible ? "Hide" : "Show";
    }

    async void OnForgotPasswordClicked(object sender, EventArgs e)
    {
        try
        {
            await Launcher.OpenAsync(_config.ForgotPasswordUrl);
        }
        catch
        {
            ShowStatus("Could not open password reset page. Visit futurecontractorsofamerica.com/login on the web.");
        }
    }

    private async Task SignInAsync()
    {
        StatusLabel.IsVisible = false;
        VerificationPanel.IsVisible = false;
        _pendingChallengeId = null;

        var email = EmailEntry.Text?.Trim().ToLowerInvariant() ?? "";
        var password = PasswordEntry.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowStatus("Enter your work email and password.");
            return;
        }

        IsBusy = true;
        SignInButton.IsEnabled = false;

        try
        {
            var result = await _api.SignInAsync(email, password);
            if (result.RequiresVerification)
            {
                _pendingChallengeId = result.ChallengeId;
                _pendingEmail = email;
                VerificationLabel.Text = result.MaskedEmail is { Length: > 0 }
                    ? $"Enter the 6-digit code sent to {result.MaskedEmail}."
                    : "Enter the 6-digit code sent to your email.";
                VerificationPanel.IsVisible = true;
                VerificationCodeEntry.Text = "";
                ShowStatus("Check your email for a verification code.");
                return;
            }

            if (!result.IsSuccessful || string.IsNullOrWhiteSpace(result.AccessToken))
            {
                ShowStatus(result.ErrorMessage ?? "We could not verify those credentials. Check your email and password.");
                return;
            }

            await CompleteSignInAsync(email, result.AccessToken);
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

    private async Task VerifyAsync()
    {
        if (string.IsNullOrWhiteSpace(_pendingChallengeId))
        {
            ShowStatus("Sign in first to receive a verification code.");
            return;
        }

        var code = VerificationCodeEntry.Text?.Trim() ?? "";
        if (code.Length < 6)
        {
            ShowStatus("Enter the 6-digit verification code.");
            return;
        }

        IsBusy = true;
        VerifyButton.IsEnabled = false;
        try
        {
            var result = await _api.VerifySignInAsync(_pendingChallengeId, code);
            if (!result.IsSuccessful || string.IsNullOrWhiteSpace(result.AccessToken))
            {
                ShowStatus(result.ErrorMessage ?? "Invalid or expired verification code.");
                return;
            }

            await CompleteSignInAsync(_pendingEmail ?? EmailEntry.Text?.Trim() ?? "", result.AccessToken);
        }
        catch
        {
            ShowStatus("Verification is temporarily unavailable. Try again shortly.");
        }
        finally
        {
            VerifyButton.IsEnabled = true;
            IsBusy = false;
        }
    }

    private async Task CompleteSignInAsync(string email, string token)
    {
        _store.Save(new CustomerProfile { Email = email });
        await _store.SaveAccessTokenAsync(token);
        PasswordEntry.Text = "";
        VerificationCodeEntry.Text = "";
        VerificationPanel.IsVisible = false;
        _pendingChallengeId = null;
        await Shell.Current.GoToAsync("//main/command");
    }

    async void OnGetStartedClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("getstarted");

    private void ShowStatus(string message)
    {
        StatusLabel.Text = message;
        StatusLabel.IsVisible = true;
    }
}
