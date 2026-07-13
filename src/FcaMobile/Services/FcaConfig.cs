namespace Fca.Mobile.Services;

public sealed class FcaConfig
{
    public string PlatformBaseUrl { get; init; } = "https://api.futurecontractorsofamerica.com";
    public string WebsiteUrl { get; init; } = "https://www.futurecontractorsofamerica.com";
    public string LoginUrl { get; init; } = "https://www.futurecontractorsofamerica.com/login";
    public string ForgotPasswordUrl { get; init; } = "https://www.futurecontractorsofamerica.com/login?forgot=1";
    public string PilotCheckoutUrl { get; init; } = "https://www.futurecontractorsofamerica.com/checkout?plan=pilot";
    public string StartupCheckoutUrl { get; init; } = "";

    /// <summary>Azure Function App host (same app as api. subdomain). Kept for diagnostics and failover.</summary>
    public string AzureFunctionHost { get; init; } = "https://auricrux-central.azurewebsites.net";

    /// <summary>Resolved API origin used by HttpClient (custom domain first).</summary>
    public string ApiOrigin => $"{PlatformBaseUrl.TrimEnd('/')}/api/";
    public string BuildPortalHandoffUrl(string portalPath)
    {
        var baseUrl = WebsiteUrl.TrimEnd('/');
        var path = portalPath.StartsWith('/') ? portalPath : "/" + portalPath;
        return $"{baseUrl}{path}";
    }

    public static FcaConfig Current { get; } = new();
}
