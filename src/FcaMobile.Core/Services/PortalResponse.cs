using System.Text.Json;

namespace Fca.Mobile.Services;

public static class PortalResponse
{
    public static bool IsEnvelopeFailure(string json, out string? errorMessage)
    {
        errorMessage = null;
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("ok", out var ok))
            return false;

        if (ok.ValueKind == JsonValueKind.True || ok.GetBoolean())
            return false;

        errorMessage = root.TryGetProperty("error", out var error)
            ? error.GetString()
            : null;
        return true;
    }

    public static bool IsMutationFailure(string json, out string? errorMessage)
    {
        if (!IsEnvelopeFailure(json, out errorMessage))
            return false;

        errorMessage ??= "Request failed.";
        return true;
    }
}
