using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

public sealed class CustomerStore
{
    private const string ProfileKey = "fca_customer_profile";
    private const string SessionEmailKey = "fca_session_email";
    private const string AuthTokenKey = "fca_auth_token";

    public CustomerProfile? Load()
    {
        var json = Preferences.Get(ProfileKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<CustomerProfile>(json);
        }
        catch (JsonException)
        {
            Preferences.Remove(ProfileKey);
            return null;
        }
    }

    public void Save(CustomerProfile profile)
    {
        Preferences.Set(ProfileKey, JsonSerializer.Serialize(new CustomerProfile
        {
            Plan = profile.Plan,
            Company = profile.Company,
            Name = profile.Name,
            Email = profile.Email,
        }));
    }

    public async Task CompleteSignInAsync(string email, string? authToken, CustomerProfile? profile = null)
    {
        var savedProfile = profile ?? Load() ?? new CustomerProfile();
        savedProfile.Email = email;

        if (!string.IsNullOrWhiteSpace(authToken))
            await SecureStorage.Default.SetAsync(AuthTokenKey, authToken);
        else
            SecureStorage.Default.Remove(AuthTokenKey);

        Save(savedProfile);
        Preferences.Set(SessionEmailKey, email);
    }

    public async Task<string?> GetAuthTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(AuthTokenKey);
        }
        catch (Exception)
        {
            SecureStorage.Default.Remove(AuthTokenKey);
            return null;
        }
    }

    public void Clear()
    {
        Preferences.Remove(ProfileKey);
        Preferences.Remove(SessionEmailKey);
        SecureStorage.Default.Remove(AuthTokenKey);
    }

    public bool IsSignedIn => !string.IsNullOrWhiteSpace(Preferences.Get(SessionEmailKey, string.Empty));
}
