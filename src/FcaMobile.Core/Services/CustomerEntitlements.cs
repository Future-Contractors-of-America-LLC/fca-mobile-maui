using System.Text.Json;

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

    private static bool IsEntitlementEnabled(JsonElement element, string key) =>
        !element.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.False;
}
