namespace Fca.Mobile.Services;

public sealed class FcaConfig
{
  public string PlatformBaseUrl { get; init; } = "https://api.futurecontractorsofamerica.com";
  public string WebsiteUrl { get; init; } = "https://futurecontractorsofamerica.com";
  public string LoginUrl { get; init; } = "https://futurecontractorsofamerica.com/login";
  public string ForgotPasswordUrl { get; init; } = "https://futurecontractorsofamerica.com/login?forgot=1";
  public string PilotCheckoutUrl { get; init; } = "https://futurecontractorsofamerica.com/checkout?plan=pilot";
  public string StartupCheckoutUrl { get; init; } = "";

  /// <summary>Azure Function App host (same app as api. subdomain). Kept for diagnostics.</summary>
  public string AzureFunctionHost { get; init; } = "https://auricrux-central.azurewebsites.net";

    public string BuildPortalHandoffUrl(string portalPath)
    {
        var baseUrl = WebsiteUrl.TrimEnd('/');
        var path = portalPath.StartsWith('/') ? portalPath : "/" + portalPath;
        return $"{baseUrl}{path}";
    }

    public static FcaConfig Current { get; } = new();
}
