namespace Fca.Mobile.Models;

public sealed record PricingTier(string Key, string Name, string Price, string Detail);

public static class PricingCatalog
{
    public static IReadOnlyList<PricingTier> Tiers { get; } =
    [
        new("startup", "Startup Workspace", "$99/mo", "Low-friction entry from lead intake through bid clarity."),
        new("starter-team", "Starter Team Workspace", "$249/mo", "Small teams with stronger precon and customer handoffs."),
        new("pilot", "Pilot Workspace", "$2,500 one-time", "Guided launch, configuration, and adoption support."),
        new("team", "Team Workspace", "$499/mo", "Daily-driver tier for active estimating and delivery teams."),
        new("operations", "Operations Workspace", "$899/mo", "Mid-size operations with projects, billing, and workforce readiness."),
        new("growth", "Growth Platform", "$1,500/mo", "Growth-stage teams expanding volume, complexity, and scale."),
        new("scale", "Scale Operations Platform", "$2,400/mo", "Multi-crew organizations preparing for enterprise standardization."),
        new("enterprise", "Enterprise Rollout", "$3,500+/mo", "Enterprise deployment with unified control and workforce transformation."),
    ];
}
