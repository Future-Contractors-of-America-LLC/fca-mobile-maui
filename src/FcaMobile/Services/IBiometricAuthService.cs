using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

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
            return await CrossFingerprint.Current.IsAvailableAsync().ConfigureAwait(false);
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
            var config = new AuthenticationRequestConfiguration("FCA Contractor Command", reason);
            var result = await CrossFingerprint.Current
                .AuthenticateAsync(config)
                .ConfigureAwait(false);
            return result.Authenticated;
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
