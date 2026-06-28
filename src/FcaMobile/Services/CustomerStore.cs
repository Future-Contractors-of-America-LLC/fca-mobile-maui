using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

public sealed class CustomerStore
{
    private const string Key = "fca_customer_profile";
    private const string AccessTokenKey = "fca_access_token";
    private const string SignedInKey = "fca_signed_in";

    public CustomerProfile? Load()
    {
        var json = Preferences.Get(Key, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var profile = JsonSerializer.Deserialize<CustomerProfile>(json);
            if (profile is not null && ContainsLegacyPassword(json))
                Save(profile);

            return profile;
        }
        catch (JsonException)
        {
            Clear();
            return null;
        }
    }

    public void Save(CustomerProfile profile)
    {
        Preferences.Set(Key, JsonSerializer.Serialize(profile));
    }

    public void Clear()
    {
        Preferences.Remove(Key);
        Preferences.Remove(AccessTokenKey);
        Preferences.Remove(SignedInKey);
        try
        {
            SecureStorage.Remove(AccessTokenKey);
        }
        catch
        {
            // Best-effort secure storage cleanup.
        }
    }

    public bool IsSignedIn =>
        Preferences.Get(SignedInKey, false) ||
        !string.IsNullOrWhiteSpace(Preferences.Get(AccessTokenKey, string.Empty)) ||
        !string.IsNullOrWhiteSpace(Load()?.Email);

    public async Task SaveAccessTokenAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Preferences.Remove(AccessTokenKey);
            Preferences.Set(SignedInKey, false);
            try
            {
                SecureStorage.Remove(AccessTokenKey);
            }
            catch
            {
                // Ignore secure storage cleanup failures.
            }

            return;
        }

        Preferences.Set(AccessTokenKey, token);
        Preferences.Set(SignedInKey, true);
        try
        {
            await SecureStorage.SetAsync(AccessTokenKey, token);
        }
        catch
        {
            // Preferences remains the source of truth when secure storage is unavailable.
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var cached = Preferences.Get(AccessTokenKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(cached))
            return cached;

        try
        {
            var secure = await SecureStorage.GetAsync(AccessTokenKey);
            if (!string.IsNullOrWhiteSpace(secure))
            {
                Preferences.Set(AccessTokenKey, secure);
                Preferences.Set(SignedInKey, true);
            }

            return secure;
        }
        catch
        {
            return null;
        }
    }

    private static bool ContainsLegacyPassword(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("Password", out _) ||
               doc.RootElement.TryGetProperty("password", out _);
    }
}
