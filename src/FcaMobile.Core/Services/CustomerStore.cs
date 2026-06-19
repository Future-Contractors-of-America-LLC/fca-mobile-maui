using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

public sealed class CustomerStore
{
    private const string ProfileKey = "fca_customer_profile";
    private const string PasswordKey = "fca_customer_password";
    private const string TokenKey = "fca_customer_token";

    private readonly IAppPreferences _preferences;
    private readonly ISecureCredentialStore _secureStore;

    public CustomerStore(IAppPreferences preferences, ISecureCredentialStore secureStore)
    {
        _preferences = preferences;
        _secureStore = secureStore;
    }

    public CustomerProfile? Load()
    {
        var json = _preferences.Get(ProfileKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<CustomerProfile>(json);
    }

    public async Task SaveAsync(CustomerProfile profile, string? password = null, string? token = null)
    {
        _preferences.Set(ProfileKey, JsonSerializer.Serialize(profile));

        if (!string.IsNullOrWhiteSpace(password))
            await _secureStore.SetAsync(PasswordKey, password).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(token))
            await _secureStore.SetAsync(TokenKey, token).ConfigureAwait(false);
    }

    public void Save(CustomerProfile profile) =>
        _preferences.Set(ProfileKey, JsonSerializer.Serialize(profile));

    public Task<string?> GetPasswordAsync() => _secureStore.GetAsync(PasswordKey);

    public Task<string?> GetTokenAsync() => _secureStore.GetAsync(TokenKey);

    public async Task ClearAsync()
    {
        _preferences.Remove(ProfileKey);
        await _secureStore.RemoveAsync(PasswordKey).ConfigureAwait(false);
        await _secureStore.RemoveAsync(TokenKey).ConfigureAwait(false);
    }

    public bool IsSignedIn => !string.IsNullOrWhiteSpace(Load()?.Email);
}
