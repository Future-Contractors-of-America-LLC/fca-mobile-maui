using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

public sealed class CustomerStore
{
    private const string Key = "fca_customer_profile";

    public CustomerProfile? Load()
    {
        var json = Preferences.Get(Key, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<CustomerProfile>(json);
        }
        catch (JsonException)
        {
            // Corrupted preference value - treat as signed out instead of crashing.
            Clear();
            return null;
        }
    }

    public void Save(CustomerProfile profile)
    {
        // Never persist the password. It is only needed transiently to authenticate;
        // storing it in plaintext preferences would expose customer credentials.
        var persisted = new CustomerProfile
        {
            Plan = profile.Plan,
            Company = profile.Company,
            Name = profile.Name,
            Email = profile.Email,
        };
        Preferences.Set(Key, JsonSerializer.Serialize(persisted));
    }

    public void Clear() => Preferences.Remove(Key);

    public bool IsSignedIn => !string.IsNullOrWhiteSpace(Load()?.Email);
}
