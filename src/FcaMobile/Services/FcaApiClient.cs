using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

public sealed record SignInResult(bool IsAuthenticated, string? AuthToken = null);

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
        _http = new HttpClient { BaseAddress = new Uri($"{config.PlatformBaseUrl.TrimEnd('/')}/api/") };
    }

    public async Task<SignInResult> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("customer-login", new { email, password }, ct);
        if (!response.IsSuccessStatusCode)
            return new SignInResult(false);

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
            return new SignInResult(false);

        return new SignInResult(true, ExtractAuthToken(doc.RootElement));
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
        await SendAuthenticatedJsonAsync("portal-messages", new { subject, message, channel }, ct);
    }

    public async Task<IReadOnlyList<PortalInvoice>> GetInvoicesAsync(CancellationToken ct = default)
        => await GetItemsAsync<PortalInvoice>("portal-invoices", ct);

    public async Task<IReadOnlyList<SupportTicket>> GetSupportCasesAsync(CancellationToken ct = default)
        => await GetItemsAsync<SupportTicket>("support-tickets", ct);

    public async Task CreateSupportCaseAsync(string subject, string priority, string detail, CancellationToken ct = default)
    {
        await SendAuthenticatedJsonAsync("support-tickets", new { subject, priority, detail }, ct);
    }

    public async Task<bool> SubmitLeadIntakeAsync(CustomerProfile profile, CancellationToken ct = default)
    {
        var value = profile.Plan switch
        {
            "pilot" => 2500,
            "startup" => 99,
            _ => 249,
        };
        await _http.PostAsJsonAsync("bids", new
        {
            company = profile.Company,
            projectName = $"{profile.Company} - {profile.Plan}",
            contactName = profile.Name,
            contactEmail = profile.Email,
            value,
            status = "new",
            source = "fca-mobile-maui",
        }, ct);
        return response.IsSuccessStatusCode;
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
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        await AddAuthHeaderAsync(request);

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadAsStringAsync(ct);
    }

    private async Task SendAuthenticatedJsonAsync<T>(string path, T payload, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload),
        };
        await AddAuthHeaderAsync(request);

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task AddAuthHeaderAsync(HttpRequestMessage request)
    {
        var token = await _store.GetAuthTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static string? ExtractAuthToken(JsonElement root)
    {
        foreach (var propertyName in new[] { "token", "accessToken", "authToken", "jwt", "sessionToken" })
        {
            if (root.TryGetProperty(propertyName, out var token) && token.ValueKind == JsonValueKind.String)
                return token.GetString();
        }

        foreach (var propertyName in new[] { "session", "auth", "user" })
        {
            if (root.TryGetProperty(propertyName, out var nested) && nested.ValueKind == JsonValueKind.Object)
            {
                var token = ExtractAuthToken(nested);
                if (!string.IsNullOrWhiteSpace(token))
                    return token;
            }
        }

        return null;
    }
}
