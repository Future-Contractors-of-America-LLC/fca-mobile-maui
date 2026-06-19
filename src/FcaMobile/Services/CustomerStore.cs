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
        return JsonSerializer.Deserialize<CustomerProfile>(json);
    }

    public void Save(CustomerProfile profile)
    {
        Preferences.Set(Key, JsonSerializer.Serialize(profile));
    }

    public void Clear() => Preferences.Remove(Key);

    public bool IsSignedIn => !string.IsNullOrWhiteSpace(Load()?.Email);
}
