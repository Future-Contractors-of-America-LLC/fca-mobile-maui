using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

public sealed class CustomerStore
{
    private const string ProfileKey = "fca_customer_profile";
    private const string SessionEmailKey = "fca_session_email";
    private const string HasPersistedAuthTokenKey = "fca_has_persisted_auth_token";
    private const string AuthTokenKey = "fca_auth_token";

    private bool _hasVerifiedSession;

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
        {
            await SecureStorage.Default.SetAsync(AuthTokenKey, authToken);
            Preferences.Set(HasPersistedAuthTokenKey, true);
        }
        else
        {
            SecureStorage.Default.Remove(AuthTokenKey);
            Preferences.Set(HasPersistedAuthTokenKey, false);
        }

        Save(savedProfile);
        Preferences.Set(SessionEmailKey, email);
        _hasVerifiedSession = true;
    }

    public async Task<string?> GetAuthTokenAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync(AuthTokenKey);
            if (string.IsNullOrWhiteSpace(token))
                Preferences.Set(HasPersistedAuthTokenKey, false);

            return token;
        }
        catch (Exception)
        {
            SecureStorage.Default.Remove(AuthTokenKey);
            Preferences.Set(HasPersistedAuthTokenKey, false);
            return null;
        }
    }

    public async Task<bool> CanRestoreSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(Preferences.Get(SessionEmailKey, string.Empty))
            || !Preferences.Get(HasPersistedAuthTokenKey, false))
            return false;

        return !string.IsNullOrWhiteSpace(await GetAuthTokenAsync());
    }

    public void Clear()
    {
        Preferences.Remove(ProfileKey);
        Preferences.Remove(SessionEmailKey);
        Preferences.Remove(HasPersistedAuthTokenKey);
        SecureStorage.Default.Remove(AuthTokenKey);
        _hasVerifiedSession = false;
    }

    public bool IsSignedIn
        => _hasVerifiedSession
           || (!string.IsNullOrWhiteSpace(Preferences.Get(SessionEmailKey, string.Empty))
               && Preferences.Get(HasPersistedAuthTokenKey, false));
}
