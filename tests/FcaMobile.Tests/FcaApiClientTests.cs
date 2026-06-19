using System.Net;
using System.Text;
using Fca.Mobile.Models;
using Fca.Mobile.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FcaMobile.Tests;

public class FcaApiClientTests
{
    [Fact]
    public async Task SignInAsync_returns_success_and_persists_token()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"ok":true,"token":"abc123"}""", Encoding.UTF8, "application/json"),
        });
        var store = new CustomerStore(new FakePreferences(), new FakeSecureStore());
        var client = CreateClient(handler, store);

        var result = await client.SignInAsync("ops@summit.com", "secret");

        Assert.True(result.IsSuccess);
        Assert.Equal("abc123", await store.GetTokenAsync());
    }

    [Fact]
    public async Task GetLeadsAsync_parses_array_payload()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """[{"projectName":"Tower retrofit","company":"Summit","status":"new","value":250000}]""",
                Encoding.UTF8,
                "application/json"),
        });
        var client = CreateClient(handler, new CustomerStore(new FakePreferences(), new FakeSecureStore()));

        var result = await client.GetLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Tower retrofit", result.Value![0].ProjectName);
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
