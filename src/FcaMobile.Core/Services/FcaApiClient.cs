using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Fca.Mobile.Models;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Services;

public sealed class FcaApiClient
{
    private readonly HttpClient _http;
    private readonly CustomerStore _store;
    private readonly INetworkStatus _networkStatus;
    private readonly ILogger<FcaApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public FcaApiClient(
        HttpClient http,
        CustomerStore store,
        INetworkStatus networkStatus,
        ILogger<FcaApiClient> logger)
    {
        _http = http;
        _store = store;
        _networkStatus = networkStatus;
        _logger = logger;
    }

    public async Task<ApiResult<bool>> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        if (_networkStatus.IsOffline())
            return ApiResult<bool>.Failure("No network connection. Check your internet and try again.");

        try
        {
            var response = await _http.PostAsJsonAsync("customer-login", new { email, password }, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return ApiResult<bool>.Failure("We could not verify those credentials. Check your email and password.");

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
                return ApiResult<bool>.Failure("We could not verify those credentials. Check your email and password.");

            var token = ExtractToken(root);
            var profile = _store.Load() ?? new CustomerProfile();
            profile.Email = email;
            await _store.SaveAsync(profile, password, token).ConfigureAwait(false);

            return ApiResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign-in failed for {Email}", email);
            return ApiResult<bool>.Failure("Unable to sign in right now. Check your connection and try again.");
        }
    }

    public async Task<ApiResult<IReadOnlyList<BidRecord>>> GetLeadsAsync(CancellationToken ct = default)
    {
        var jsonResult = await GetRawAsync("bids", ct).ConfigureAwait(false);
        if (!jsonResult.IsSuccess)
            return ApiResult<IReadOnlyList<BidRecord>>.Failure(jsonResult.ErrorMessage!);

        var json = jsonResult.Value!;
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            var items = JsonSerializer.Deserialize<List<BidRecord>>(json, JsonOptions) ?? new List<BidRecord>();
            return ApiResult<IReadOnlyList<BidRecord>>.Success(items);
        }

        if (doc.RootElement.TryGetProperty("items", out var itemsElement))
        {
            var items = JsonSerializer.Deserialize<List<BidRecord>>(itemsElement.GetRawText(), JsonOptions)
                ?? new List<BidRecord>();
            return ApiResult<IReadOnlyList<BidRecord>>.Success(items);
        }

        return ApiResult<IReadOnlyList<BidRecord>>.Success(Array.Empty<BidRecord>());
    }

    public Task<ApiResult<IReadOnlyList<ProjectRecord>>> GetJobsAsync(CancellationToken ct = default) =>
        GetItemsAsync<ProjectRecord>("projects", ct);

    public Task<ApiResult<IReadOnlyList<FileRecord>>> GetDocumentsAsync(CancellationToken ct = default) =>
        GetItemsAsync<FileRecord>("files", ct);

    public async Task<ApiResult<AcademySnapshot>> GetTrainingAsync(CancellationToken ct = default)
    {
        var jsonResult = await GetRawAsync("academy-lms", ct).ConfigureAwait(false);
        if (!jsonResult.IsSuccess)
            return ApiResult<AcademySnapshot>.Failure(jsonResult.ErrorMessage!);

        var json = jsonResult.Value!;
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("catalog", out var catalog))
        {
            return ApiResult<AcademySnapshot>.Success(new AcademySnapshot
            {
                Catalog = JsonSerializer.Deserialize<AcademyCatalog>(catalog.GetRawText(), JsonOptions),
            });
        }

        var snapshot = JsonSerializer.Deserialize<AcademySnapshot>(json, JsonOptions);
        return ApiResult<AcademySnapshot>.Success(snapshot ?? new AcademySnapshot());
    }

    public Task<ApiResult<IReadOnlyList<PortalMessage>>> GetMessagesAsync(CancellationToken ct = default) =>
        GetItemsAsync<PortalMessage>("portal-messages", ct);

    public async Task<ApiResult> SendMessageAsync(
        string subject,
        string message,
        string channel,
        CancellationToken ct = default)
    {
        if (_networkStatus.IsOffline())
            return ApiResult.Failure("No network connection. Check your internet and try again.");

        try
        {
            await ApplyAuthHeadersAsync().ConfigureAwait(false);
            var response = await _http.PostAsJsonAsync("portal-messages", new { subject, message, channel }, ct)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? ApiResult.Success()
                : ApiResult.Failure("Unable to send your message. Try again in a moment.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send portal message");
            return ApiResult.Failure("Unable to send your message. Check your connection and try again.");
        }
    }

    public Task<ApiResult<IReadOnlyList<PortalInvoice>>> GetInvoicesAsync(CancellationToken ct = default) =>
        GetItemsAsync<PortalInvoice>("portal-invoices", ct);

    public Task<ApiResult<IReadOnlyList<SupportTicket>>> GetSupportCasesAsync(CancellationToken ct = default) =>
        GetItemsAsync<SupportTicket>("support-tickets", ct);

    public async Task<ApiResult> CreateSupportCaseAsync(
        string subject,
        string priority,
        string detail,
        CancellationToken ct = default)
    {
        if (_networkStatus.IsOffline())
            return ApiResult.Failure("No network connection. Check your internet and try again.");

        try
        {
            await ApplyAuthHeadersAsync().ConfigureAwait(false);
            var response = await _http.PostAsJsonAsync("support-tickets", new { subject, priority, detail }, ct)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? ApiResult.Success()
                : ApiResult.Failure("Unable to open your support case. Try again in a moment.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create support case");
            return ApiResult.Failure("Unable to open your support case. Check your connection and try again.");
        }
    }

    public async Task<ApiResult> SubmitLeadIntakeAsync(CustomerProfile profile, CancellationToken ct = default)
    {
        if (_networkStatus.IsOffline())
            return ApiResult.Failure("No network connection. Check your internet and try again.");

        try
        {
            var value = profile.Plan switch
            {
                "pilot" => 2500,
                "startup" => 99,
                _ => 249,
            };

            await ApplyAuthHeadersAsync().ConfigureAwait(false);
            var response = await _http.PostAsJsonAsync("bids", new
            {
                company = profile.Company,
                projectName = $"{profile.Company} - {profile.Plan}",
                contactName = profile.Name,
                contactEmail = profile.Email,
                value,
                status = "new",
                source = "fca-mobile-maui",
            }, ct).ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? ApiResult.Success()
                : ApiResult.Failure("Unable to submit your workspace request. Try again in a moment.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit lead intake for {Company}", profile.Company);
            return ApiResult.Failure("Unable to submit your workspace request. Check your connection and try again.");
        }
    }

    private async Task<ApiResult<IReadOnlyList<T>>> GetItemsAsync<T>(string path, CancellationToken ct)
    {
        var jsonResult = await GetRawAsync(path, ct).ConfigureAwait(false);
        if (!jsonResult.IsSuccess)
            return ApiResult<IReadOnlyList<T>>.Failure(jsonResult.ErrorMessage!);

        var json = jsonResult.Value!;
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("items", out var items))
        {
            var list = JsonSerializer.Deserialize<List<T>>(items.GetRawText(), JsonOptions) ?? new List<T>();
            return ApiResult<IReadOnlyList<T>>.Success(list);
        }

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            var list = JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
            return ApiResult<IReadOnlyList<T>>.Success(list);
        }

        return ApiResult<IReadOnlyList<T>>.Success(Array.Empty<T>());
    }

    private async Task<ApiResult<string>> GetRawAsync(string path, CancellationToken ct)
    {
        if (_networkStatus.IsOffline())
            return ApiResult<string>.Failure("No network connection. Check your internet and try again.");

        try
        {
            await ApplyAuthHeadersAsync().ConfigureAwait(false);
            var response = await _http.GetAsync(path, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API request to {Path} failed with status {StatusCode}", path, response.StatusCode);
                return ApiResult<string>.Failure("Unable to load data from FCA right now. Pull to refresh and try again.");
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return ApiResult<string>.Success(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API request to {Path} failed", path);
            return ApiResult<string>.Failure("Unable to reach FCA services. Check your connection and try again.");
        }
    }

    private async Task ApplyAuthHeadersAsync()
    {
        _http.DefaultRequestHeaders.Authorization = null;

        var token = await _store.GetTokenAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return;
        }

        var profile = _store.Load();
        var password = await _store.GetPasswordAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(profile?.Email) && !string.IsNullOrWhiteSpace(password))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{profile.Email}:{password}")));
        }
    }

    private static string? ExtractToken(JsonElement root)
    {
        foreach (var propertyName in new[] { "token", "accessToken", "sessionToken", "authToken" })
        {
            if (root.TryGetProperty(propertyName, out var tokenElement))
            {
                var value = tokenElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return null;
    }
}
