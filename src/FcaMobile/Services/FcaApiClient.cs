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

public sealed record SignInResult(
    bool IsSuccessful,
    string? AccessToken,
    string? ErrorMessage,
    int StatusCode,
    bool RequiresVerification = false,
    string? ChallengeId = null,
    string? MaskedEmail = null);

public sealed class FcaApiClient
{
    private readonly HttpClient _http;
    private readonly CustomerStore _store;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions WriteJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public FcaApiClient(FcaConfig config, CustomerStore store)
    {
        _store = store;
        _http = new HttpClient
        {
            BaseAddress = new Uri($"{config.PlatformBaseUrl.TrimEnd('/')}/api/"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "FCA-Mobile-MAUI/1.0");
    }

    public async Task<SignInResult> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        // Dictionary keys stay lowercase regardless of serializer naming quirks on device runtimes.
        var payload = new Dictionary<string, string>
        {
            ["email"] = (email ?? "").Trim().ToLowerInvariant(),
            ["password"] = password ?? "",
        };
        using var content = new StringContent(
            JsonSerializer.Serialize(payload, WriteJsonOptions),
            System.Text.Encoding.UTF8,
            "application/json");
        using var response = await _http.PostAsync("customer-login", content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = TryParseJson(json);
        if (doc is null)
        {
            // #region agent log
            AgentLog("H4", "sign-in invalid json", new { status = (int)response.StatusCode, baseAddress = _http.BaseAddress?.ToString() });
            // #endregion
            return new SignInResult(false, null, "Sign in returned an invalid response.", (int)response.StatusCode);
        }

        var root = doc.RootElement;
        if (!response.IsSuccessStatusCode || (root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.False))
        {
            var error = root.TryGetProperty("error", out var errorNode) && errorNode.ValueKind == JsonValueKind.String
                ? errorNode.GetString()
                : null;
            // #region agent log
            AgentLog("H1", "sign-in rejected", new
            {
                status = (int)response.StatusCode,
                error,
                email = payload["email"],
                baseAddress = _http.BaseAddress?.ToString(),
            });
            // #endregion
            return new SignInResult(false, null, error ?? "We could not verify those credentials.", (int)response.StatusCode);
        }

        if (root.TryGetProperty("requiresVerification", out var requiresVerification)
            && requiresVerification.ValueKind == JsonValueKind.True)
        {
            var challengeId = root.TryGetProperty("challengeId", out var cid) && cid.ValueKind == JsonValueKind.String
                ? cid.GetString()
                : null;
            var maskedEmail = root.TryGetProperty("maskedEmail", out var masked) && masked.ValueKind == JsonValueKind.String
                ? masked.GetString()
                : null;
            // #region agent log
            AgentLog("H3", "sign-in requires verification", new { status = (int)response.StatusCode, challengeId });
            // #endregion
            return new SignInResult(
                false,
                null,
                null,
                (int)response.StatusCode,
                RequiresVerification: true,
                ChallengeId: challengeId,
                MaskedEmail: maskedEmail);
        }

        var token = TryReadAccessToken(root);
        if (string.IsNullOrWhiteSpace(token))
        {
            // #region agent log
            AgentLog("H2", "sign-in missing session token", new { status = (int)response.StatusCode });
            // #endregion
            return new SignInResult(false, null, "Sign in succeeded but no session token was returned.", (int)response.StatusCode);
        }

        // #region agent log
        AgentLog("H2", "sign-in succeeded", new { status = (int)response.StatusCode, hasToken = true });
        // #endregion
        return new SignInResult(true, token, null, (int)response.StatusCode);
    }

    public async Task<SignInResult> VerifySignInAsync(string challengeId, string code, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, string>
        {
            ["challengeId"] = challengeId ?? "",
            ["code"] = code ?? "",
        };
        using var content = new StringContent(
            JsonSerializer.Serialize(payload, WriteJsonOptions),
            System.Text.Encoding.UTF8,
            "application/json");
        using var response = await _http.PostAsync("customer-verify", content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = TryParseJson(json);
        if (doc is null)
            return new SignInResult(false, null, "Verification returned an invalid response.", (int)response.StatusCode);

        var root = doc.RootElement;
        if (!response.IsSuccessStatusCode || (root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.False))
        {
            var error = root.TryGetProperty("error", out var errorNode) && errorNode.ValueKind == JsonValueKind.String
                ? errorNode.GetString()
                : "Invalid or expired verification code.";
            return new SignInResult(false, null, error, (int)response.StatusCode);
        }

        var token = TryReadAccessToken(root);
        if (string.IsNullOrWhiteSpace(token))
            return new SignInResult(false, null, "Verification succeeded but no session token was returned.", (int)response.StatusCode);

        return new SignInResult(true, token, null, (int)response.StatusCode);
    }

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = await CreateRequestAsync<object>(HttpMethod.Post, "customer-logout", null, ct);
            await _http.SendAsync(request, ct);
        }
        catch
        {
            // best-effort logout
        }
    }

    public async Task<bool> HasActiveSessionAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = await CreateRequestAsync<object>(HttpMethod.Get, "customer-session", null, ct);
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("authenticated", out var authenticated) && authenticated.GetBoolean();
        }
        catch
        {
            return false;
        }
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

    public async Task QualifyLeadAsync(string bidId, CancellationToken ct = default)
    {
        await PatchAsync("bids", new
        {
            action = "update-qualification",
            bidId,
            updates = new
            {
                status = "Qualified",
                score = "88/100",
                checklist = new
                {
                    plansReceived = true,
                    siteWalkComplete = true,
                    budgetConfirmed = true,
                    decisionMakerIdentified = true,
                    tradeLevelingComplete = true,
                    jurisdictionReviewed = true,
                },
            },
            detail = "Mobile contractor qualified lead on FCA spine.",
        }, ct);
    }

    public async Task<IReadOnlyList<FieldTaskRecord>> GetFieldTasksAsync(CancellationToken ct = default)
        => await GetItemsAsync<FieldTaskRecord>("field-tasks", ct);

    private async Task PatchAsync<T>(string path, T payload, CancellationToken ct)
    {
        using var request = await CreateRequestAsync(HttpMethod.Patch, path, payload, ct);
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new FcaApiException(path, response.StatusCode);
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
            request.Content = JsonContent.Create(payload, options: WriteJsonOptions);

        ct.ThrowIfCancellationRequested();
        return request;
    }

    private static string? TryReadAccessToken(JsonElement root)
    {
        foreach (var propertyName in new[] { "sessionToken", "accessToken", "token", "jwt" })
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

    // #region agent log
    private static void AgentLog(string hypothesisId, string message, object data)
    {
        System.Diagnostics.Debug.WriteLine($"[0210c8][{hypothesisId}] {message} {JsonSerializer.Serialize(data)}");
        try
        {
            Preferences.Set("fca_debug_signin", JsonSerializer.Serialize(new
            {
                hypothesisId,
                message,
                data,
                at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            }));
        }
        catch
        {
            // Best-effort debug capture for device repro.
        }
    }
    // #endregion
}
