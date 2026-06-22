using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace FcaMobile.Tests;

public class CustomerStoreTests
{
    [Fact]
    public void IsSignedIn_returns_false_when_profile_missing()
    {
        var store = new CustomerStore(new FakePreferences(), new FakeSecureStore());

        Assert.False(store.IsSignedIn);
    }

    [Fact]
    public async Task SaveAsync_persists_profile_and_secure_credentials()
    {
        var preferences = new FakePreferences();
        var secureStore = new FakeSecureStore();
        var store = new CustomerStore(preferences, secureStore);
        var profile = new CustomerProfile
        {
            Company = "Summit Builders",
            Email = "ops@summit.com",
            Plan = "pilot",
        };

        await store.SaveAsync(profile, "secret-password", "session-token");

        var loaded = store.Load();
        Assert.NotNull(loaded);
        Assert.Equal("Summit Builders", loaded!.Company);
        Assert.Equal("ops@summit.com", loaded.Email);
        Assert.DoesNotContain("secret-password", preferences.Get("fca_customer_profile", string.Empty));
        Assert.Equal("secret-password", await secureStore.GetAsync("fca_customer_password"));
        Assert.Equal("session-token", await secureStore.GetAsync("fca_session_token"));
        Assert.True(store.IsSignedIn);
    }

    [Fact]
    public async Task ClearAsync_removes_profile_and_credentials()
    {
        var preferences = new FakePreferences();
        var secureStore = new FakeSecureStore();
        var store = new CustomerStore(preferences, secureStore);
        await store.SaveAsync(new CustomerProfile { Email = "ops@summit.com" }, "secret-password", "session-token");

        await store.ClearAsync();

        Assert.Null(store.Load());
        Assert.Null(await secureStore.GetAsync("fca_customer_password"));
        Assert.Null(await secureStore.GetAsync("fca_session_token"));
        Assert.False(store.IsSignedIn);
    }
}

internal sealed class FakePreferences : IAppPreferences
{
    private readonly Dictionary<string, string> _values = new();

    public string Get(string key, string defaultValue) =>
        _values.TryGetValue(key, out var value) ? value : defaultValue;

    public void Set(string key, string value) => _values[key] = value;

    public void Remove(string key) => _values.Remove(key);
}

internal sealed class FakeSecureStore : ISecureCredentialStore
{
    private readonly Dictionary<string, string> _values = new();

    public Task SetAsync(string key, string value)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key) =>
        Task.FromResult(_values.TryGetValue(key, out var value) ? value : null);

    public Task RemoveAsync(string key)
    {
        _values.Remove(key);
        return Task.CompletedTask;
    }
}
