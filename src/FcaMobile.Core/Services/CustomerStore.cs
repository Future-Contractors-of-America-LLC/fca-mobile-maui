using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

public sealed class CustomerStore
{
    private const string ProfileKey = "fca_customer_profile";
    private const string PasswordKey = "fca_customer_password";
    private const string SessionKey = "fca_session_token";

    private static readonly JsonSerializerOptions ProfileJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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

        var profile = JsonSerializer.Deserialize<CustomerProfile>(json, ProfileJsonOptions);
        if (profile is not null)
            CustomerEntitlements.ApplyToProfile(profile);

        return profile;
    }

    public async Task SaveAsync(CustomerProfile profile, string? password = null, string? sessionToken = null)
    {
        _preferences.Set(ProfileKey, JsonSerializer.Serialize(profile, ProfileJsonOptions));

        if (!string.IsNullOrWhiteSpace(password))
            await _secureStore.SetAsync(PasswordKey, password).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(sessionToken))
            await _secureStore.SetAsync(SessionKey, sessionToken).ConfigureAwait(false);
    }

    public void Save(CustomerProfile profile) =>
        _preferences.Set(ProfileKey, JsonSerializer.Serialize(profile, ProfileJsonOptions));

    public Task<string?> GetPasswordAsync() => _secureStore.GetAsync(PasswordKey);

    public Task<string?> GetSessionTokenAsync() => _secureStore.GetAsync(SessionKey);

    public async Task ClearAsync()
    {
        _preferences.Remove(ProfileKey);
        await _secureStore.RemoveAsync(PasswordKey).ConfigureAwait(false);
        await _secureStore.RemoveAsync(SessionKey).ConfigureAwait(false);
    }

    public bool IsSignedIn => !string.IsNullOrWhiteSpace(Load()?.Email);
}
