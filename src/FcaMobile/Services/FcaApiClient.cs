using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Fca.Mobile.Models;

namespace Fca.Mobile.Services;

public sealed class FcaApiClient
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public FcaApiClient(FcaConfig config)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri($"{config.PlatformBaseUrl.TrimEnd('/')}/api/"),
            Timeout = RequestTimeout,
        };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<bool> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("customer-login", new { email, password }, ct);
            if (!response.IsSuccessStatusCode)
                return false;
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("ok", out var ok) && ok.GetBoolean();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<BidRecord>> GetLeadsAsync(CancellationToken ct = default)
    {
        var json = await GetRawAsync("bids", ct);
        if (json is null)
            return Array.Empty<BidRecord>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return JsonSerializer.Deserialize<List<BidRecord>>(json, JsonOptions) ?? new List<BidRecord>();

            if (doc.RootElement.TryGetProperty("items", out var items))
                return JsonSerializer.Deserialize<List<BidRecord>>(items.GetRawText(), JsonOptions) ?? new List<BidRecord>();
        }
        catch (JsonException)
        {
            // Malformed payload - degrade to an empty list rather than crashing the UI.
        }

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
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<PortalMessage>> GetMessagesAsync(CancellationToken ct = default)
        => await GetItemsAsync<PortalMessage>("portal-messages", ct);

    public Task<bool> SendMessageAsync(string subject, string message, string channel, CancellationToken ct = default)
        => PostAsync("portal-messages", new { subject, message, channel }, ct);

    public async Task<IReadOnlyList<PortalInvoice>> GetInvoicesAsync(CancellationToken ct = default)
        => await GetItemsAsync<PortalInvoice>("portal-invoices", ct);

    public async Task<IReadOnlyList<SupportTicket>> GetSupportCasesAsync(CancellationToken ct = default)
        => await GetItemsAsync<SupportTicket>("support-tickets", ct);

    public Task<bool> CreateSupportCaseAsync(string subject, string priority, string detail, CancellationToken ct = default)
        => PostAsync("support-tickets", new { subject, priority, detail }, ct);

    public Task<bool> SubmitLeadIntakeAsync(CustomerProfile profile, CancellationToken ct = default)
    {
        var value = profile.Plan switch
        {
            "pilot" => 2500,
            "startup" => 99,
            _ => 249,
        };
        return PostAsync("bids", new
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

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("items", out var items))
                return JsonSerializer.Deserialize<List<T>>(items.GetRawText(), JsonOptions) ?? new List<T>();

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
        }
        catch (JsonException)
        {
            // Malformed payload - degrade to an empty list rather than crashing the UI.
        }

        return Array.Empty<T>();
    }

    private async Task<string?> GetRawAsync(string path, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync(path, ct);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return null;
        }
    }

    private async Task<bool> PostAsync(string path, object payload, CancellationToken ct)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(path, payload, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return false;
        }
    }
}
