using Plugin.Maui.Biometric;

namespace Fca.Mobile.Services;

public interface IBiometricAuthService
{
    Task<bool> IsAvailableAsync();

    Task<bool> AuthenticateAsync(string reason);

    bool IsEnabled { get; }

    Task SetEnabledAsync(bool enabled);
}

public sealed class MauiBiometricAuthService : IBiometricAuthService
{
    private const string EnabledKey = "fca_biometric_enabled";

    public bool IsEnabled => Preferences.Get(EnabledKey, false);

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var status = await BiometricAuthenticationService.Default
                .GetAuthenticationStatusAsync()
                .ConfigureAwait(false);
            return status == BiometricAuthenticationStatus.Available;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync(string reason)
    {
        try
        {
            var result = await BiometricAuthenticationService.Default
                .AuthenticateAsync(
                    new AuthenticationRequest
                    {
                        Title = "FCA Contractor Command",
                        Reason = reason,
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);
            return result.IsSuccessful;
        }
        catch
        {
            return false;
        }
    }

    public Task SetEnabledAsync(bool enabled)
    {
        Preferences.Set(EnabledKey, enabled);
        return Task.CompletedTask;
    }
}
