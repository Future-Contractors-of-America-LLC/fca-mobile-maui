using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Fca.Mobile.Models;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Services;

public sealed class FcaApiClient
{
    private const string SessionCookieName = "fca_session";

    private readonly HttpClient _http;
    private readonly CustomerStore _store;
    private readonly IFcaApiHostResolver _hostResolver;
    private readonly INetworkStatus _networkStatus;
    private readonly ILogger<FcaApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Regex SessionCookieRegex = new(
        $"{SessionCookieName}=([^;]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public FcaApiClient(
        HttpClient http,
        CustomerStore store,
        IFcaApiHostResolver hostResolver,
        INetworkStatus networkStatus,
        ILogger<FcaApiClient> logger)
    {
        _http = http;
        _store = store;
        _hostResolver = hostResolver;
        _networkStatus = networkStatus;
        _logger = logger;
    }

    public async Task<ApiResult<bool>> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        if (_networkStatus.IsOffline())
            return ApiResult<bool>.Failure("No network connection. Check your internet and try again.");

        try
        {
            await EnsureHostAsync(ct).ConfigureAwait(false);

            using var request = new HttpRequestMessage(HttpMethod.Post, "customer-login")
            {
                Content = JsonContent.Create(new { email, password }),
            };

            var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return ApiResult<bool>.Failure("We could not verify those credentials. Check your email and password.");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
                return ApiResult<bool>.Failure("We could not verify those credentials. Check your email and password.");

            if (!root.TryGetProperty("account", out var accountElement) || accountElement.ValueKind != JsonValueKind.Object)
                return ApiResult<bool>.Failure("Sign in succeeded but your workspace account was not returned. Try again.");

            var sessionToken = ExtractSessionToken(response);
            if (string.IsNullOrWhiteSpace(sessionToken))
                return ApiResult<bool>.Failure("Sign in succeeded but no server session was issued. Try again.");

            var profile = ParseAccountProfile(accountElement, email);
            await _store.SaveAsync(profile, password, sessionToken).ConfigureAwait(false);

            return ApiResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign-in failed for {Email}", email);
            return ApiResult<bool>.Failure("Unable to sign in right now. Check your connection and try again.");
        }
    }

    public Task EnsurePlatformReadyAsync(CancellationToken ct = default) =>
        EnsureHostAsync(ct);

    public async Task<ApiResult<bool>> SyncSessionAsync(CancellationToken ct = default)
    {
        if (_networkStatus.IsOffline())
            return ApiResult<bool>.Failure("No network connection. Check your internet and try again.");

        try
        {
            await EnsureHostAsync(ct).ConfigureAwait(false);

            using var request = new HttpRequestMessage(HttpMethod.Get, "customer-session");
            await ApplySessionCookieAsync(request).ConfigureAwait(false);

            var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return ApiResult<bool>.Failure("Unable to verify your session.");

            if (PortalResponse.IsEnvelopeFailure(json, out var sessionError))
                return ApiResult<bool>.Failure(sessionError ?? "Unable to verify your session.");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("authenticated", out var authenticated) || !authenticated.GetBoolean())
                return ApiResult<bool>.Failure("Your session has expired. Sign in again.");

            if (!root.TryGetProperty("account", out var accountElement))
                return ApiResult<bool>.Success(true);

            var existing = _store.Load() ?? new CustomerProfile();
            var profile = ParseAccountProfile(accountElement, existing.Email);
            await _store.SaveAsync(profile).ConfigureAwait(false);

            return ApiResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session sync failed");
            return ApiResult<bool>.Failure("Unable to verify your session.");
        }
    }

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureHostAsync(ct).ConfigureAwait(false);

            using var request = new HttpRequestMessage(HttpMethod.Post, "customer-logout");
            await ApplySessionCookieAsync(request).ConfigureAwait(false);
            await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Customer logout request failed");
        }
        finally
        {
            await _store.ClearAsync().ConfigureAwait(false);
        }
    }

    public async Task<ApiResult<IReadOnlyList<BidRecord>>> GetLeadsAsync(CancellationToken ct = default) =>
        await GetItemsAsync<BidRecord>("bids", ct).ConfigureAwait(false);

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

        if (root.TryGetProperty("programs", out var programs))
        {
            return ApiResult<AcademySnapshot>.Success(new AcademySnapshot
            {
                Catalog = new AcademyCatalog
                {
                    Programs = JsonSerializer.Deserialize<List<AcademyProgram>>(programs.GetRawText(), JsonOptions),
                },
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
            await EnsureHostAsync(ct).ConfigureAwait(false);

            using var request = new HttpRequestMessage(HttpMethod.Post, "portal-messages")
            {
                Content = JsonContent.Create(new { subject, message, channel }),
            };
            await ApplySessionCookieAsync(request).ConfigureAwait(false);

            var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return ApiResult.Failure("Unable to send your message. Try again in a moment.");

            if (PortalResponse.IsMutationFailure(json, out var error))
                return ApiResult.Failure(error ?? "Unable to send your message. Try again in a moment.");

            return ApiResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send portal message");
            return ApiResult.Failure("Unable to send your message. Check your connection and try again.");
        }
    }

    public Task<ApiResult<IReadOnlyList<PortalInvoice>>> GetInvoicesAsync(CancellationToken ct = default) =>
        GetItemsAsync<PortalInvoice>("portal-invoices", ct);

    public async Task<ApiResult<BillingSummarySnapshot>> GetBillingSummaryAsync(CancellationToken ct = default)
    {
        var jsonResult = await GetRawAsync("billing-summary", ct).ConfigureAwait(false);
        if (!jsonResult.IsSuccess)
            return ApiResult<BillingSummarySnapshot>.Failure(jsonResult.ErrorMessage!);

        var json = jsonResult.Value!;
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var snapshot = new BillingSummarySnapshot
        {
            Count = root.TryGetProperty("count", out var count) ? count.GetInt32() : 0,
            Items = root.TryGetProperty("items", out var items)
                ? JsonSerializer.Deserialize<List<BillingSummaryRecord>>(items.GetRawText(), JsonOptions)
                : new List<BillingSummaryRecord>(),
        };

        return ApiResult<BillingSummarySnapshot>.Success(snapshot);
    }

    public async Task RegisterMobileDeviceAsync(
        string platform,
        string appVersion,
        string bundleId,
        CancellationToken ct = default)
    {
        if (_networkStatus.IsOffline())
            return;

        try
        {
            await EnsureHostAsync(ct).ConfigureAwait(false);

            using var request = new HttpRequestMessage(HttpMethod.Post, "mobile/register")
            {
                Content = JsonContent.Create(new
                {
                    platform,
                    appVersion,
                    bundleId,
                    source = "fca-mobile-maui",
                }),
            };
            await ApplySessionCookieAsync(request).ConfigureAwait(false);

            var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Mobile register returned status {StatusCode}", response.StatusCode);
                return;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (PortalResponse.IsMutationFailure(json, out var error))
                _logger.LogWarning("Mobile register failed: {Error}", error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mobile device registration failed");
        }
    }

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
            await EnsureHostAsync(ct).ConfigureAwait(false);

            using var request = new HttpRequestMessage(HttpMethod.Post, "support-tickets")
            {
                Content = JsonContent.Create(new { subject, priority, detail }),
            };
            await ApplySessionCookieAsync(request).ConfigureAwait(false);

            var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return ApiResult.Failure("Unable to open your support case. Try again in a moment.");

            if (PortalResponse.IsMutationFailure(json, out var error))
                return ApiResult.Failure(error ?? "Unable to open your support case. Try again in a moment.");

            return ApiResult.Success();
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
            await EnsureHostAsync(ct).ConfigureAwait(false);

            var value = PlanCatalog.IntakeValue(profile.Plan);
            var intakeId = Guid.NewGuid().ToString("N");

            using var bidRequest = new HttpRequestMessage(HttpMethod.Post, "bids")
            {
                Content = JsonContent.Create(new
                {
                    company = profile.Company,
                    projectName = $"{profile.Company} - {profile.Plan}",
                    contactName = profile.Name,
                    contactEmail = profile.Email,
                    value,
                    status = "new",
                    intakeId,
                    source = "fca-mobile-maui",
                }),
            };
            await ApplySessionCookieAsync(bidRequest).ConfigureAwait(false);

            var bidResponse = await _http.SendAsync(bidRequest, ct).ConfigureAwait(false);
            var bidJson = await bidResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!bidResponse.IsSuccessStatusCode)
                return ApiResult.Failure("Unable to submit your workspace request. Try again in a moment.");

            if (PortalResponse.IsMutationFailure(bidJson, out var bidError))
                return ApiResult.Failure(bidError ?? "Unable to submit your workspace request. Try again in a moment.");

            await MirrorLeadIntakeAsync(profile, value, ct).ConfigureAwait(false);

            return ApiResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit lead intake for {Company}", profile.Company);
            return ApiResult.Failure("Unable to submit your workspace request. Check your connection and try again.");
        }
    }

    private async Task MirrorLeadIntakeAsync(CustomerProfile profile, int value, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "leads")
            {
                Content = JsonContent.Create(new
                {
                    sourceChannel = "fca-mobile-maui",
                    serviceLine = "general-construction",
                    projectIntent = profile.Plan,
                    sourceRoute = "mobile/getstarted",
                    createdBy = "fca-mobile-maui",
                    client = new
                    {
                        name = profile.Company,
                        contactName = profile.Name,
                        contactEmail = profile.Email,
                    },
                    site = new
                    {
                        name = $"{profile.Company} - {profile.Plan}",
                        estimatedValue = value,
                    },
                    notes = $"Mobile intake plan: {profile.Plan}",
                }),
            };
            await ApplySessionCookieAsync(request).ConfigureAwait(false);
            await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lead mirror failed for {Company}", profile.Company);
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
            await EnsureHostAsync(ct).ConfigureAwait(false);

            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            await ApplySessionCookieAsync(request).ConfigureAwait(false);

            var response = await _http.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API request to {Path} failed with status {StatusCode}", path, response.StatusCode);
                return ApiResult<string>.Failure("Unable to load data from FCA right now. Pull to refresh and try again.");
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (PortalResponse.IsEnvelopeFailure(content, out var error))
            {
                _logger.LogWarning("API request to {Path} returned ok=false: {Error}", path, error);
                return ApiResult<string>.Failure(error ?? "Unable to load data from FCA right now. Pull to refresh and try again.");
            }

            return ApiResult<string>.Success(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API request to {Path} failed", path);
            return ApiResult<string>.Failure("Unable to reach FCA services. Check your connection and try again.");
        }
    }

    private async Task ApplySessionCookieAsync(HttpRequestMessage request)
    {
        var sessionToken = await _store.GetSessionTokenAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(sessionToken))
            return;

        request.Headers.Remove("Cookie");
        request.Headers.TryAddWithoutValidation("Cookie", $"{SessionCookieName}={sessionToken}");
    }

    private static string? ExtractSessionToken(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            return null;

        foreach (var cookie in setCookies)
        {
            var match = SessionCookieRegex.Match(cookie);
            if (match.Success)
                return match.Groups[1].Value;
        }

        return null;
    }

    private static CustomerProfile ParseAccountProfile(JsonElement account, string fallbackEmail)
    {
        var profile = new CustomerProfile
        {
            Email = ReadString(account, "email") ?? fallbackEmail,
            Company = ReadString(account, "company") ?? "",
            Name = ReadString(account, "contactName") ?? ReadString(account, "name") ?? "",
            Plan = ReadString(account, "selectedPlan") ?? "startup",
            CustomerId = ReadString(account, "customerId") ?? "",
            Role = ReadString(account, "role") ?? "",
            WorkspaceLabel = ReadString(account, "workspaceLabel") ?? "",
        };

        profile.EnabledProducts = CustomerEntitlements.NormalizeProducts(
            account.TryGetProperty("enabledProducts", out var products) ? products : null);

        profile.EnabledComms = CustomerEntitlements.NormalizeComms(
            account.TryGetProperty("enabledComms", out var comms) ? comms : null);

        return profile;
    }

    private Task EnsureHostAsync(CancellationToken ct) =>
        _hostResolver.EnsureResolvedAsync(_http, ct);

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
}
