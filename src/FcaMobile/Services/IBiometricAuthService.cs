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
    public bool IsEnabled => false;

    public Task<bool> IsAvailableAsync() => Task.FromResult(false);

    public Task<bool> AuthenticateAsync(string reason) => Task.FromResult(false);

    public Task SetEnabledAsync(bool enabled) => Task.CompletedTask;
}
