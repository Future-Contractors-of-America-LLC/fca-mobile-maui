using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

public sealed class FcaApiException : Exception
{
    public FcaApiException(string path, HttpStatusCode statusCode)
        : base($"FCA API request to '{path}' failed with status {(int)statusCode} ({statusCode}).")
    {
        Path = path;
        StatusCode = statusCode;
    }

    public string Path { get; }
    public HttpStatusCode StatusCode { get; }
}

public sealed record SignInResult(bool IsSuccessful, string? AccessToken);

public sealed class FcaApiClient
{
    private readonly HttpClient _http;
    private readonly CustomerStore _store;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public FcaApiClient(FcaConfig config, CustomerStore store)
    {
        _store = store;
        _http = new HttpClient
        {
            BaseAddress = new Uri($"{config.PlatformBaseUrl.TrimEnd('/')}/api/"),
            Timeout = TimeSpan.FromSeconds(30),
        };
    }

    public async Task<SignInResult> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync("customer-login", new { email, password }, ct);
        if (!response.IsSuccessStatusCode)
            return new SignInResult(false, null);

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = TryParseJson(json);
        if (doc is null)
            return new SignInResult(false, null);

        var root = doc.RootElement;
        if (root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.False)
            return new SignInResult(false, null);

        return new SignInResult(true, TryReadAccessToken(root));
    }

    public async Task<IReadOnlyList<BidRecord>> GetLeadsAsync(CancellationToken ct = default)
    {
        var json = await GetRawAsync("bids", ct);
        if (json is null)
            return Array.Empty<BidRecord>();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<BidRecord>>(json, JsonOptions) ?? new List<BidRecord>();

        if (doc.RootElement.TryGetProperty("items", out var items))
            return JsonSerializer.Deserialize<List<BidRecord>>(items.GetRawText(), JsonOptions) ?? new List<BidRecord>();

        return Array.Empty<BidRecord>();
    }

    public async Task<IReadOnlyList<ProjectRecord>> GetJobsAsync(CancellationToken ct = default)
        => await GetItemsAsync<ProjectRecord>("projects", ct);

    public async Task<IReadOnlyList<FileRecord>> GetDocumentsAsync(CancellationToken ct = default)
        => await GetItemsAsync<FileRecord>("files", ct);

    public async Task<AcademySnapshot?> GetTrainingAsync(CancellationToken ct = default)
    {
        var json = await GetRawAsync("academy-lms", ct);
        if (json is null)
            return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("catalog", out var catalog))
        {
            return new AcademySnapshot
            {
                Catalog = JsonSerializer.Deserialize<AcademyCatalog>(catalog.GetRawText(), JsonOptions),
            };
        }

        return JsonSerializer.Deserialize<AcademySnapshot>(json, JsonOptions);
    }

    public async Task<IReadOnlyList<PortalMessage>> GetMessagesAsync(CancellationToken ct = default)
        => await GetItemsAsync<PortalMessage>("portal-messages", ct);

    public async Task SendMessageAsync(string subject, string message, string channel, CancellationToken ct = default)
    {
        await PostAsync("portal-messages", new { subject, message, channel }, ct);
    }

    public async Task<IReadOnlyList<PortalInvoice>> GetInvoicesAsync(CancellationToken ct = default)
        => await GetItemsAsync<PortalInvoice>("portal-invoices", ct);

    public async Task<IReadOnlyList<SupportTicket>> GetSupportCasesAsync(CancellationToken ct = default)
        => await GetItemsAsync<SupportTicket>("support-tickets", ct);

    public async Task CreateSupportCaseAsync(string subject, string priority, string detail, CancellationToken ct = default)
    {
        await PostAsync("support-tickets", new { subject, priority, detail }, ct);
    }

    public async Task SubmitLeadIntakeAsync(CustomerProfile profile, CancellationToken ct = default)
    {
        var value = profile.Plan switch
        {
            "pilot" => 2500,
            "startup" => 99,
            _ => 249,
        };
        await PostAsync("bids", new
        {
            company = profile.Company,
            projectName = $"{profile.Company} - {profile.Plan}",
            contactName = profile.Name,
            contactEmail = profile.Email,
            value,
            status = "new",
            source = "fca-mobile-maui",
        }, ct);
    }

    private async Task<IReadOnlyList<T>> GetItemsAsync<T>(string path, CancellationToken ct)
    {
        var json = await GetRawAsync(path, ct);
        if (json is null)
            return Array.Empty<T>();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("items", out var items))
            return JsonSerializer.Deserialize<List<T>>(items.GetRawText(), JsonOptions) ?? new List<T>();

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();

        return Array.Empty<T>();
    }

    private async Task<string?> GetRawAsync(string path, CancellationToken ct)
    {
        using var request = await CreateRequestAsync<object>(HttpMethod.Get, path, null, ct);
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new FcaApiException(path, response.StatusCode);

        return await response.Content.ReadAsStringAsync(ct);
    }

    private async Task PostAsync<T>(string path, T payload, CancellationToken ct)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, path, payload, ct);
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new FcaApiException(path, response.StatusCode);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync<T>(HttpMethod method, string path, T? payload, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, path);
        var token = await _store.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (payload is not null)
            request.Content = JsonContent.Create(payload, options: JsonOptions);

        ct.ThrowIfCancellationRequested();
        return request;
    }

    private static string? TryReadAccessToken(JsonElement root)
    {
        foreach (var propertyName in new[] { "accessToken", "token", "jwt" })
        {
            if (root.TryGetProperty(propertyName, out var token) && token.ValueKind == JsonValueKind.String)
                return token.GetString();
        }

        if (root.TryGetProperty("session", out var session) && session.ValueKind == JsonValueKind.Object)
            return TryReadAccessToken(session);

        return null;
    }

    private static JsonDocument? TryParseJson(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
