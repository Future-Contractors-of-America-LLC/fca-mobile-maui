namespace Fca.Mobile.Services;

public static class PlanCatalog
{
    public static readonly IReadOnlyList<string> IntakePlans =
    [
        "startup",
        "starter-team",
        "pilot",
        "team",
        "operations",
        "growth",
        "scale-operations",
        "enterprise",
    ];

    public static int IntakeValue(string plan) => plan switch
    {
        "pilot" => 2500,
        "startup" => 99,
        _ => 249,
    };

    public static string CheckoutUrl(FcaConfig config, string plan, string? email = null)
    {
        if (plan == "pilot")
            return config.PilotCheckoutUrl;

        if (plan == "enterprise")
            return $"{config.WebsiteUrl.TrimEnd('/')}/contact";

        var query = $"plan={Uri.EscapeDataString(plan)}";
        if (!string.IsNullOrWhiteSpace(email))
            query += $"&email={Uri.EscapeDataString(email)}";

        return $"{config.WebsiteUrl.TrimEnd('/')}/checkout?{query}";
    }
}
