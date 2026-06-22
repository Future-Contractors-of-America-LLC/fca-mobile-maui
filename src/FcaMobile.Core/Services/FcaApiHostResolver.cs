using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Services;

public sealed class FcaApiHostResolver : IFcaApiHostResolver
{
    private readonly FcaConfig _config;
    private readonly ILogger<FcaApiHostResolver> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _resolvedOrigin;
    private bool _initialized;

    public FcaApiHostResolver(FcaConfig config, ILogger<FcaApiHostResolver> logger)
    {
        _config = config;
        _logger = logger;
    }

    public string ApiOrigin => _resolvedOrigin ?? _config.PlatformBaseUrl;

    public Uri ApiBaseUri => new($"{ApiOrigin.TrimEnd('/')}/api/");

    public async Task EnsureResolvedAsync(HttpClient apiClient, CancellationToken ct = default)
    {
        if (_initialized)
        {
            apiClient.BaseAddress = ApiBaseUri;
            return;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                apiClient.BaseAddress = ApiBaseUri;
                return;
            }

            _resolvedOrigin = await ProbeApiOriginAsync(ct).ConfigureAwait(false);
            apiClient.BaseAddress = ApiBaseUri;
            _initialized = true;
            _logger.LogInformation("FCA API host resolved to {ApiOrigin}", _resolvedOrigin);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> ProbeApiOriginAsync(CancellationToken ct)
    {
        foreach (var origin in new[] { _config.PlatformBaseUrl, _config.PlatformFallbackUrl })
        {
            if (await IsHealthyAsync(origin, ct).ConfigureAwait(false))
                return origin.TrimEnd('/');
        }

        _logger.LogWarning(
            "Neither primary nor fallback API health checks succeeded; defaulting to {Primary}",
            _config.PlatformBaseUrl);
        return _config.PlatformBaseUrl.TrimEnd('/');
    }

    private async Task<bool> IsHealthyAsync(string origin, CancellationToken ct)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            var healthUri = new Uri($"{origin.TrimEnd('/')}/api/health");
            using var response = await client.GetAsync(healthUri, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health probe failed for {Origin}", origin);
            return false;
        }
    }
}
