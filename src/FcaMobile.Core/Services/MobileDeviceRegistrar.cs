namespace Fca.Mobile.Services;

public sealed class MobileDeviceRegistrar
{
    public const string RegisteredKey = "fca_mobile_registered";

    private readonly IAppPreferences _preferences;
    private readonly FcaApiClient _api;

    public MobileDeviceRegistrar(IAppPreferences preferences, FcaApiClient api)
    {
        _preferences = preferences;
        _api = api;
    }

    public async Task RegisterIfNeededAsync(
        string platform,
        string appVersion,
        string bundleId,
        CancellationToken ct = default)
    {
        if (_preferences.Get(RegisteredKey, "false") == "true")
            return;

        await _api.RegisterMobileDeviceAsync(platform, appVersion, bundleId, ct).ConfigureAwait(false);
        _preferences.Set(RegisteredKey, "true");
    }
}
