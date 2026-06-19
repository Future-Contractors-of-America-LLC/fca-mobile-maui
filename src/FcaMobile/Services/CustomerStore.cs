using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

public sealed class CustomerStore
{
    private const string Key = "fca_customer_profile";
    private const string AccessTokenKey = "fca_access_token";

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
        SecureStorage.Remove(AccessTokenKey);
    }

    public bool IsSignedIn => !string.IsNullOrWhiteSpace(Load()?.Email);

    public async Task SaveAccessTokenAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            SecureStorage.Remove(AccessTokenKey);
            return;
        }

        await SecureStorage.SetAsync(AccessTokenKey, token);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await SecureStorage.GetAsync(AccessTokenKey);
        }
        catch
        {
            SecureStorage.Remove(AccessTokenKey);
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
