using System.Net;
using System.Text;
using Fca.Mobile.Models;
using Fca.Mobile.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FcaMobile.Tests;

public class FcaApiClientTests
{
    [Fact]
    public async Task SignInAsync_persists_fca_session_cookie_and_account()
    {
        const string sessionToken = "encoded.payload.signature";
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Headers = { { "Set-Cookie", $"fca_session={sessionToken}; HttpOnly; Path=/" } },
            Content = new StringContent(
                """
                {
                  "ok": true,
                  "account": {
                    "email": "ops@summit.com",
                    "company": "Summit Builders",
                    "customerId": "TEN-001",
                    "selectedPlan": "startup",
                    "enabledProducts": ["SaaS workspace"],
                    "enabledComms": { "email": true, "chat": true }
                  }
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });
        var store = new CustomerStore(new FakePreferences(), new FakeSecureStore());
        var client = CreateClient(handler, store);

        var result = await client.SignInAsync("ops@summit.com", "secret");

        Assert.True(result.IsSuccess);
        Assert.Equal(sessionToken, await store.GetSessionTokenAsync());
        Assert.Equal("Summit Builders", store.Load()?.Company);
        Assert.Equal("startup", store.Load()?.Plan);
    }

    [Fact]
    public async Task GetLeadsAsync_parses_bid_tracker_shape()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Contains("fca_session=test-session", request.Headers.GetValues("Cookie").First());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "ok": true,
                      "items": [
                        {
                          "id": "BID-1",
                          "package": "Tower retrofit",
                          "scopePackage": "Electrical package",
                          "status": "Quoted",
                          "value": "$148,000",
                          "nextCommercialMove": "Route to estimator"
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            };
        });

        var store = new CustomerStore(new FakePreferences(), new FakeSecureStore());
        await store.SaveAsync(new CustomerProfile { Email = "ops@summit.com" }, sessionToken: "test-session");
        var client = CreateClient(handler, store);

        var result = await client.GetLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Tower retrofit", result.Value![0].Package);
        Assert.Equal("$148,000", result.Value![0].Value);
        Assert.Equal("Route to estimator", result.Value![0].NextCommercialMove);
    }

    [Fact]
    public async Task GetLeadsAsync_returns_failure_for_non_success_status()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = CreateClient(handler, new CustomerStore(new FakePreferences(), new FakeSecureStore()));

        var result = await client.GetLeadsAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("Unable to load data", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static FcaApiClient CreateClient(HttpMessageHandler handler, CustomerStore store)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/api/"),
        };

        return new FcaApiClient(
            httpClient,
            store,
            new OnlineNetworkStatus(),
            NullLogger<FcaApiClient>.Instance);
    }
}

internal sealed class OnlineNetworkStatus : INetworkStatus
{
    public bool IsOffline() => false;
}

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        _responder = responder;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(_responder(request));
}
