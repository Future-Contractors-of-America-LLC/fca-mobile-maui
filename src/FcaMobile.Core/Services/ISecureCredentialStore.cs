namespace Fca.Mobile.Services;

public interface ISecureCredentialStore
{
    Task SetAsync(string key, string value);

    Task<string?> GetAsync(string key);

    Task RemoveAsync(string key);
}
