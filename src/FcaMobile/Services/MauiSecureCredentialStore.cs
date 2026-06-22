namespace Fca.Mobile.Services;

public sealed class MauiSecureCredentialStore : ISecureCredentialStore
{
    public Task SetAsync(string key, string value) => SecureStorage.SetAsync(key, value);

    public Task<string?> GetAsync(string key) => SecureStorage.GetAsync(key);

    public Task RemoveAsync(string key)
    {
        SecureStorage.Remove(key);
        return Task.CompletedTask;
    }
}
