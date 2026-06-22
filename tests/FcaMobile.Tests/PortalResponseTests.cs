using Fca.Mobile.Services;

namespace FcaMobile.Tests;

public class PortalResponseTests
{
    [Fact]
    public void IsEnvelopeFailure_detects_ok_false()
    {
        var failed = PortalResponse.IsEnvelopeFailure("""{ "ok": false, "error": "Tenant unavailable" }""", out var error);

        Assert.True(failed);
        Assert.Equal("Tenant unavailable", error);
    }

    [Fact]
    public void IsEnvelopeFailure_ignores_success_payload()
    {
        var failed = PortalResponse.IsEnvelopeFailure("""{ "ok": true, "items": [] }""", out _);

        Assert.False(failed);
    }

    [Fact]
    public void IsEnvelopeFailure_ignores_non_object_payload()
    {
        var failed = PortalResponse.IsEnvelopeFailure("""[1, 2, 3]""", out _);

        Assert.False(failed);
    }
}
