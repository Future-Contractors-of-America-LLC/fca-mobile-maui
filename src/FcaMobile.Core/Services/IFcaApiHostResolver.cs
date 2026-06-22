namespace Fca.Mobile.Services;

public interface IFcaApiHostResolver
{
    string ApiOrigin { get; }

    Uri ApiBaseUri { get; }

    Task EnsureResolvedAsync(HttpClient apiClient, CancellationToken ct = default);
}
