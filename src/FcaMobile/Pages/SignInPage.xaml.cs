using Fca.Mobile.Models;
using Fca.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Pages;

public partial class SignInPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;
    private readonly ILogger<SignInPage> _logger;

    public SignInPage(FcaApiClient api, CustomerStore store, ILogger<SignInPage> logger)
    {
        _api = api;
        _store = store;
        _logger = logger;
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

        SetBusy(true);
        try
        {
            var ok = await _api.SignInAsync(email, password);
            if (!ok)
            {
                StatusLabel.Text = "We could not verify those credentials. Check your email and password.";
                StatusLabel.IsVisible = true;
                return;
            }

            var profile = _store.Load() ?? new CustomerProfile();
            profile.Email = email;
            _store.Save(profile);
            await _store.SaveCredentialsAsync(email, password);

            await Shell.Current.GoToAsync("//main/command");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign-in failed unexpectedly");
            StatusLabel.Text = "A network error occurred. Please check your connection and try again.";
            StatusLabel.IsVisible = true;
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void OnGetStartedClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("getstarted");

    void SetBusy(bool busy)
    {
        LoadingIndicator.IsRunning = busy;
        LoadingIndicator.IsVisible = busy;
        SignInButton.IsEnabled = !busy;
        GetStartedButton.IsEnabled = !busy;
        EmailEntry.IsEnabled = !busy;
        PasswordEntry.IsEnabled = !busy;
    }
}
