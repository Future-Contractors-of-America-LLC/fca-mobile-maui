using System.Net.Http.Json;
using System.Text.Json;
using Fca.Mobile.Models;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Services;

public sealed class FcaApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<FcaApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public FcaApiClient(FcaConfig config, ILogger<FcaApiClient> logger)
        : this(CreateHttpClient(config), logger) { }

    /// <summary>Testable constructor — accepts a pre-configured <see cref="HttpClient"/>.</summary>
    internal FcaApiClient(HttpClient http, ILogger<FcaApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    private static HttpClient CreateHttpClient(FcaConfig config) => new()
    {
        BaseAddress = new Uri($"{config.PlatformBaseUrl.TrimEnd('/')}/api/"),
        Timeout = TimeSpan.FromSeconds(30),
    };

    public async Task<bool> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("customer-login", new { email, password }, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sign-in returned {StatusCode}", response.StatusCode);
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("ok", out var ok) && ok.GetBoolean();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "SignInAsync failed");
            return false;
        }
    }

    public async Task<IReadOnlyList<BidRecord>> GetLeadsAsync(CancellationToken ct = default)
    {
        var json = await GetRawAsync("bids", ct);
        if (json is null)
            return [];

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return JsonSerializer.Deserialize<List<BidRecord>>(json, JsonOptions) ?? [];

            if (doc.RootElement.TryGetProperty("items", out var items))
                return JsonSerializer.Deserialize<List<BidRecord>>(items.GetRawText(), JsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse leads response");
        }

        return [];
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

        try
        {
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
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse training response");
            return null;
        }
    }

    public async Task<IReadOnlyList<PortalMessage>> GetMessagesAsync(CancellationToken ct = default)
        => await GetItemsAsync<PortalMessage>("portal-messages", ct);

    /// <returns>True if the message was accepted by the server.</returns>
    public async Task<bool> SendMessageAsync(string subject, string message, string channel, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("portal-messages", new { subject, message, channel }, ct);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("SendMessage returned {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "SendMessageAsync failed");
            return false;
        }
    }

    public async Task<IReadOnlyList<PortalInvoice>> GetInvoicesAsync(CancellationToken ct = default)
        => await GetItemsAsync<PortalInvoice>("portal-invoices", ct);

    public async Task<IReadOnlyList<SupportTicket>> GetSupportCasesAsync(CancellationToken ct = default)
        => await GetItemsAsync<SupportTicket>("support-tickets", ct);

    /// <returns>True if the support case was accepted by the server.</returns>
    public async Task<bool> CreateSupportCaseAsync(string subject, string priority, string detail, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("support-tickets", new { subject, priority, detail }, ct);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("CreateSupportCase returned {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "CreateSupportCaseAsync failed");
            return false;
        }
    }

    /// <returns>True if the lead intake was accepted by the server.</returns>
    public async Task<bool> SubmitLeadIntakeAsync(CustomerProfile profile, CancellationToken ct = default)
    {
        var value = profile.Plan switch
        {
            "pilot" => 2500,
            "startup" => 99,
            _ => 249,
        };

        try
        {
            var response = await _http.PostAsJsonAsync("bids", new
            {
                company = profile.Company,
                projectName = $"{profile.Company} - {profile.Plan}",
                contactName = profile.Name,
                contactEmail = profile.Email,
                value,
                status = "new",
                source = "fca-mobile-maui",
            }, ct);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("SubmitLeadIntake returned {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "SubmitLeadIntakeAsync failed");
            return false;
        }
    }

    private async Task<IReadOnlyList<T>> GetItemsAsync<T>(string path, CancellationToken ct)
    {
        var json = await GetRawAsync(path, ct);
        if (json is null)
            return [];

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("items", out var items))
                return JsonSerializer.Deserialize<List<T>>(items.GetRawText(), JsonOptions) ?? [];

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse {Path} response", path);
        }

        return [];
    }

    private async Task<string?> GetRawAsync(string path, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync(path, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GET {Path} returned {StatusCode}", path, response.StatusCode);
                return null;
            }
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "GET {Path} timed out", path);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GET {Path} network error", path);
            return null;
        }
    }
}
