namespace Fca.Mobile.Services;

public sealed class MauiPreferences : IAppPreferences
{
    public string Get(string key, string defaultValue) => Preferences.Get(key, defaultValue);

    public void Set(string key, string value) => Preferences.Set(key, value);

    public void Remove(string key) => Preferences.Remove(key);
}
