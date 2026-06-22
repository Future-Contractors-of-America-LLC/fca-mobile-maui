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
                    "enabledProducts": { "saas": true, "lms": false, "auricrux": true },
                    "enabledComms": { "email": true, "chat": true, "sms": false }
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
        Assert.True(store.Load()?.EnabledProducts?["saas"]);
        Assert.False(store.Load()?.EnabledProducts?["lms"]);
        Assert.False(store.Load()?.EnabledComms?["sms"]);
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

    [Fact]
    public async Task GetLeadsAsync_returns_failure_when_ok_is_false()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{ "ok": false, "error": "Session expired" }""", Encoding.UTF8, "application/json"),
        });
        var client = CreateClient(handler, new CustomerStore(new FakePreferences(), new FakeSecureStore()));

        var result = await client.GetLeadsAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("Session expired", result.ErrorMessage);
    }

    [Fact]
    public async Task GetBillingSummaryAsync_parses_billing_summary_shape()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "ok": true,
                  "count": 1,
                  "items": [
                    {
                      "projectId": "A-117",
                      "contractValue": "$148,000",
                      "billedToDate": "$42,000",
                      "outstandingToBill": "$28,000",
                      "collectionsStatus": "Current"
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });
        var client = CreateClient(handler, new CustomerStore(new FakePreferences(), new FakeSecureStore()));

        var result = await client.GetBillingSummaryAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Count);
        Assert.Equal("A-117", result.Value.Items![0].ProjectId);
    }

    [Fact]
    public async Task SyncSessionAsync_fails_when_ok_is_false()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{ "ok": false, "error": "Invalid session", "authenticated": false }""",
                Encoding.UTF8,
                "application/json"),
        });
        var store = new CustomerStore(new FakePreferences(), new FakeSecureStore());
        await store.SaveAsync(new CustomerProfile { Email = "ops@summit.com" }, sessionToken: "test-session");
        var client = CreateClient(handler, store);

        var result = await client.SyncSessionAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid session", result.ErrorMessage);
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
            new FakeHostResolver(),
            new OnlineNetworkStatus(),
            NullLogger<FcaApiClient>.Instance);
    }
}

internal sealed class FakeHostResolver : IFcaApiHostResolver
{
    public string ApiOrigin => "https://example.test";

    public Uri ApiBaseUri => new("https://example.test/api/");

    public Task EnsureResolvedAsync(HttpClient apiClient, CancellationToken ct = default)
    {
        apiClient.BaseAddress = ApiBaseUri;
        return Task.CompletedTask;
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
