using System.Text.Json;
using Fca.Mobile.Services;

namespace FcaMobile.Tests;

public class CustomerEntitlementsTests
{
    [Fact]
    public void NormalizeProducts_treats_missing_keys_as_enabled()
    {
        using var doc = JsonDocument.Parse("""{ "saas": true, "lms": false }""");
        var products = CustomerEntitlements.NormalizeProducts(doc.RootElement);

        Assert.True(products["saas"]);
        Assert.False(products["lms"]);
        Assert.True(products["auricrux"]);
    }

    [Fact]
    public void NormalizeComms_matches_bid_tracker_default_semantics()
    {
        using var doc = JsonDocument.Parse("""{ "email": true, "sms": false, "chat": true }""");
        var comms = CustomerEntitlements.NormalizeComms(doc.RootElement);

        Assert.True(comms["email"]);
        Assert.False(comms["sms"]);
        Assert.True(comms["phone"]);
        Assert.True(comms["teams"]);
    }

    [Fact]
    public void GetEnabledCommsChannels_filters_pending_channels()
    {
        var comms = new Dictionary<string, bool>
        {
            ["email"] = true,
            ["sms"] = false,
            ["chat"] = true,
            ["phone"] = false,
            ["teams"] = true,
            ["conference"] = true,
            ["lecture"] = true,
        };

        var enabled = CustomerEntitlements.GetEnabledCommsChannels(comms);

        Assert.Equal(["chat", "email", "teams", "conference", "lecture"], enabled);
    }
}
