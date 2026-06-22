using Fca.Mobile.Services;

namespace FcaMobile.Tests;

public class PlanCatalogTests
{
    [Theory]
    [InlineData("startup", 99)]
    [InlineData("pilot", 2500)]
    [InlineData("starter-team", 249)]
    [InlineData("team", 249)]
    public void IntakeValue_matches_fca_bid_tracker(string plan, int expected) =>
        Assert.Equal(expected, PlanCatalog.IntakeValue(plan));

    [Fact]
    public void CheckoutUrl_uses_integrated_checkout_for_startup()
    {
        var config = FcaConfig.Current;
        var url = PlanCatalog.CheckoutUrl(config, "startup", "ops@summit.com");

        Assert.Contains("/checkout?plan=startup", url, StringComparison.Ordinal);
        Assert.Contains("email=ops%40summit.com", url, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckoutUrl_uses_contact_for_enterprise() =>
        Assert.Contains("/contact", PlanCatalog.CheckoutUrl(FcaConfig.Current, "enterprise"), StringComparison.Ordinal);
}
