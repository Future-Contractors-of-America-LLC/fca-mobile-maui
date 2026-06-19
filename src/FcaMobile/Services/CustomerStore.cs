using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

/// <summary>
/// Persists non-sensitive customer profile data in MAUI Preferences and
/// stores credentials (password) in the platform secure enclave via SecureStorage.
/// </summary>
public sealed class CustomerStore
{
    private const string ProfileKey = "fca_customer_profile";
    private const string SecureEmailKey = "fca_email";
    private const string SecurePasswordKey = "fca_password";

    // ── Profile (non-sensitive) ──────────────────────────────────────────────

    public CustomerProfile? Load()
    {
        var json = Preferences.Get(ProfileKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return null;
        return JsonSerializer.Deserialize<CustomerProfile>(json);
    }

    public void Save(CustomerProfile profile)
    {
        // Password is [JsonIgnore] so it will never be written to Preferences.
        Preferences.Set(ProfileKey, JsonSerializer.Serialize(profile));
    }

    // ── Credentials (SecureStorage) ──────────────────────────────────────────

    public async Task SaveCredentialsAsync(string email, string password)
    {
        await SecureStorage.SetAsync(SecureEmailKey, email);
        await SecureStorage.SetAsync(SecurePasswordKey, password);
    }

    public async Task<(string Email, string Password)?> LoadCredentialsAsync()
    {
        var email = await SecureStorage.GetAsync(SecureEmailKey);
        var password = await SecureStorage.GetAsync(SecurePasswordKey);
        return string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)
            ? null
            : (email, password);
    }

    // ── Session ──────────────────────────────────────────────────────────────

    public bool IsSignedIn => !string.IsNullOrWhiteSpace(Load()?.Email);

    public void Clear()
    {
        Preferences.Remove(ProfileKey);
        SecureStorage.Remove(SecureEmailKey);
        SecureStorage.Remove(SecurePasswordKey);
    }
}
