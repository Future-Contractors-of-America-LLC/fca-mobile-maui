namespace Fca.Mobile.Services;

public sealed class FcaConfig
{
    public string PlatformBaseUrl { get; init; } = "https://auricrux-central.azurewebsites.net";
    public string WebsiteUrl { get; init; } = "https://futurecontractorsofamerica.com";
    public string PilotCheckoutUrl { get; init; } = "https://buy.stripe.com/bJe14o0fQ5Pn8Tt7Bw5gc01";
    public string StartupCheckoutUrl { get; init; } = "";

    public string BuildPortalHandoffUrl(string portalPath)
    {
        var baseUrl = WebsiteUrl.TrimEnd('/');
        var path = portalPath.StartsWith('/') ? portalPath : "/" + portalPath;
        return $"{baseUrl}{path}";
    }

    public static FcaConfig Current { get; } = new();
}
