using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

public static class CustomerEntitlements
{
    public static readonly string[] ProductKeys = ["saas", "lms", "auricrux"];

    public static readonly string[] CommsChannels =
        ["chat", "sms", "phone", "email", "teams", "conference", "lecture"];

    public static Dictionary<string, bool> DefaultProducts() => new()
    {
        ["saas"] = true,
        ["lms"] = true,
        ["auricrux"] = true,
    };

    public static Dictionary<string, bool> DefaultComms() => new()
    {
        ["chat"] = true,
        ["sms"] = true,
        ["phone"] = true,
        ["email"] = true,
        ["teams"] = true,
        ["conference"] = true,
        ["lecture"] = true,
    };

    public static Dictionary<string, bool> NormalizeProducts(JsonElement? products)
    {
        if (products is null || products.Value.ValueKind != JsonValueKind.Object)
            return DefaultProducts();

        var element = products.Value;
        return new Dictionary<string, bool>
        {
            ["saas"] = IsEntitlementEnabled(element, "saas"),
            ["lms"] = IsEntitlementEnabled(element, "lms"),
            ["auricrux"] = IsEntitlementEnabled(element, "auricrux"),
        };
    }

    public static Dictionary<string, bool> NormalizeComms(JsonElement? comms)
    {
        if (comms is null || comms.Value.ValueKind != JsonValueKind.Object)
            return DefaultComms();

        var element = comms.Value;
        var normalized = new Dictionary<string, bool>();
        foreach (var channel in CommsChannels)
            normalized[channel] = IsEntitlementEnabled(element, channel);

        return normalized;
    }

    public static bool IsProductEnabled(Dictionary<string, bool>? products, string product) =>
        products?.GetValueOrDefault(product, true) != false;

    public static bool IsCommEnabled(Dictionary<string, bool>? comms, string channel) =>
        comms?.GetValueOrDefault(channel, true) != false;

    public static IReadOnlyList<string> GetEnabledCommsChannels(Dictionary<string, bool>? comms) =>
        CommsChannels.Where(channel => IsCommEnabled(comms, channel)).ToArray();

    public static string SummarizeProducts(Dictionary<string, bool>? products)
    {
        var labels = new List<string>();
        if (IsProductEnabled(products, "saas"))
            labels.Add("SaaS workspace");
        if (IsProductEnabled(products, "lms"))
            labels.Add("Academy / LMS");
        if (IsProductEnabled(products, "auricrux"))
            labels.Add("Auricrux guidance");

        return labels.Count == 0 ? "No product access enabled" : string.Join(" · ", labels);
    }

    public static string SummarizeComms(Dictionary<string, bool>? comms)
    {
        var enabled = CommsChannels.Where(channel => IsCommEnabled(comms, channel)).ToArray();
        return enabled.Length == 0 ? "No communications enabled" : string.Join(" · ", enabled);
    }

    public static void ApplyToProfile(CustomerProfile profile)
    {
        profile.EnabledProducts = NormalizeProductsFromStored(profile.EnabledProducts);
        profile.EnabledComms = NormalizeCommsFromStored(profile.EnabledComms);
    }

    public static Dictionary<string, bool> NormalizeProductsFromStored(Dictionary<string, bool>? products)
    {
        if (products is null || products.Count == 0)
            return DefaultProducts();

        return new Dictionary<string, bool>
        {
            ["saas"] = products.GetValueOrDefault("saas", true),
            ["lms"] = products.GetValueOrDefault("lms", true),
            ["auricrux"] = products.GetValueOrDefault("auricrux", true),
        };
    }

    public static Dictionary<string, bool> NormalizeCommsFromStored(Dictionary<string, bool>? comms)
    {
        if (comms is null || comms.Count == 0)
            return DefaultComms();

        var normalized = new Dictionary<string, bool>();
        foreach (var channel in CommsChannels)
            normalized[channel] = comms.GetValueOrDefault(channel, true);

        return normalized;
    }

    private static bool IsEntitlementEnabled(JsonElement element, string key) =>
        !element.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.False;
}
