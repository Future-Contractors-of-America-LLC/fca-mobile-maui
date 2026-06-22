using Fca.Mobile.Services;

namespace FcaMobile.Tests;

public class FcaApiHostResolverTests
{
    [Fact]
    public async Task EnsureResolvedAsync_sets_http_client_base_address()
    {
        var resolver = new FcaApiHostResolver(FcaConfig.Current, Microsoft.Extensions.Logging.Abstractions.NullLogger<FcaApiHostResolver>.Instance);
        using var client = new HttpClient();

        await resolver.EnsureResolvedAsync(client);

        Assert.NotNull(client.BaseAddress);
        Assert.EndsWith("/api/", client.BaseAddress!.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
