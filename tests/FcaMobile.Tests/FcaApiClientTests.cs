using System.Net;
using System.Text.Json;
using Fca.Mobile.Models;
using Fca.Mobile.Services;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

namespace FcaMobile.Tests;

public sealed class FcaApiClientTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static FcaApiClient BuildClient(MockHttpMessageHandler handler)
    {
        var http = handler.ToHttpClient();
        http.BaseAddress = new Uri("https://test.example/api/");
        return new FcaApiClient(http, NullLogger<FcaApiClient>.Instance);
    }

    // ── SignInAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SignInAsync_ReturnsFalse_WhenServerReturnsNonSuccess()
    {
        var handler = new MockHttpMessageHandler();
        handler.When("*customer-login").Respond(HttpStatusCode.Unauthorized);

        var client = BuildClient(handler);
        var result = await client.SignInAsync("a@b.com", "wrong");

        Assert.False(result);
    }

    [Fact]
    public async Task SignInAsync_ReturnsFalse_WhenOkIsFalse()
    {
        var handler = new MockHttpMessageHandler();
        handler.When("*customer-login")
               .Respond("application/json", """{"ok":false}""");

        var client = BuildClient(handler);
        var result = await client.SignInAsync("a@b.com", "wrong");

        Assert.False(result);
    }

    [Fact]
    public async Task SignInAsync_ReturnsTrue_WhenOkIsTrue()
    {
        var handler = new MockHttpMessageHandler();
        handler.When("*customer-login")
               .Respond("application/json", """{"ok":true}""");

        var client = BuildClient(handler);
        var result = await client.SignInAsync("a@b.com", "correct");

        Assert.True(result);
    }

    [Fact]
    public async Task SignInAsync_ReturnsFalse_OnNetworkError()
    {
        var handler = new MockHttpMessageHandler();
        handler.When("*customer-login").Throw(new HttpRequestException("network down"));

        var client = BuildClient(handler);
        var result = await client.SignInAsync("a@b.com", "pass");

        Assert.False(result);
    }

    // ── GetLeadsAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLeadsAsync_ReturnsEmptyList_WhenServerReturnsNonSuccess()
    {
        var handler = new MockHttpMessageHandler();
        handler.When("*bids").Respond(HttpStatusCode.InternalServerError);

        var client = BuildClient(handler);
        var leads = await client.GetLeadsAsync();

        Assert.Empty(leads);
    }

    [Fact]
    public async Task GetLeadsAsync_ParsesTopLevelArray()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { id = "1", company = "Acme", projectName = "Project A", status = "open", value = 10000m },
        });
        var handler = new MockHttpMessageHandler();
        handler.When("*bids").Respond("application/json", json);

        var client = BuildClient(handler);
        var leads = await client.GetLeadsAsync();

        Assert.Single(leads);
        Assert.Equal("Acme", leads[0].Company);
        Assert.Equal("Project A", leads[0].ProjectName);
    }

    [Fact]
    public async Task GetLeadsAsync_ParsesItemsEnvelope()
    {
        var payload = new
        {
            items = new[]
            {
                new { id = "1", company = "Corp", projectName = "Corp HQ", status = "new", value = 5000m },
                new { id = "2", company = "LLC",  projectName = "LLC Fit-out", status = "bid", value = 2500m },
            }
        };
        var json = JsonSerializer.Serialize(payload);
        var handler = new MockHttpMessageHandler();
        handler.When("*bids").Respond("application/json", json);

        var client = BuildClient(handler);
        var leads = await client.GetLeadsAsync();

        Assert.Equal(2, leads.Count);
    }

    // ── GetJobsAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetJobsAsync_ReturnsEmptyList_WhenServerErrors()
    {
        var handler = new MockHttpMessageHandler();
        handler.When("*projects").Respond(HttpStatusCode.ServiceUnavailable);

        var client = BuildClient(handler);
        var jobs = await client.GetJobsAsync();

        Assert.Empty(jobs);
    }

    [Fact]
    public async Task GetJobsAsync_ParsesProjects()
    {
        var json = JsonSerializer.Serialize(new { items = new[] { new { id = "p1", name = "Skyline", stage = "foundation", nextStep = "pour" } } });
        var handler = new MockHttpMessageHandler();
        handler.When("*projects").Respond("application/json", json);

        var client = BuildClient(handler);
        var jobs = await client.GetJobsAsync();

        Assert.Single(jobs);
        Assert.Equal("Skyline", jobs[0].Name);
    }

    // ── GetDocumentsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetDocumentsAsync_ReturnsEmptyList_WhenServerErrors()
    {
        var handler = new MockHttpMessageHandler();
        handler.When("*files").Respond(HttpStatusCode.NotFound);

        var client = BuildClient(handler);
        var docs = await client.GetDocumentsAsync();

        Assert.Empty(docs);
    }

    // ── GetMessagesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetMessagesAsync_ParsesMessages()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { id = "m1", subject = "Hello", message = "World", channel = "portal" },
        });
        var handler = new MockHttpMessageHandler();
        handler.When("*portal-messages").Respond("application/json", json);

        var client = BuildClient(handler);
        var messages = await client.GetMessagesAsync();

        Assert.Single(messages);
        Assert.Equal("Hello", messages[0].Subject);
    }

    // ── SendMessageAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessageAsync_ReturnsTrue_WhenServerAccepts()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "*portal-messages").Respond(HttpStatusCode.OK);

        var client = BuildClient(handler);
        var result = await client.SendMessageAsync("Sub", "Body", "portal");

        Assert.True(result);
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsFalse_WhenServerRejects()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "*portal-messages").Respond(HttpStatusCode.BadRequest);

        var client = BuildClient(handler);
        var result = await client.SendMessageAsync("Sub", "Body", "portal");

        Assert.False(result);
    }

    // ── CreateSupportCaseAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CreateSupportCaseAsync_ReturnsTrue_WhenServerAccepts()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "*support-tickets").Respond(HttpStatusCode.Created);

        var client = BuildClient(handler);
        var result = await client.CreateSupportCaseAsync("Subject", "standard", "Detail");

        Assert.True(result);
    }

    // ── SubmitLeadIntakeAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task SubmitLeadIntakeAsync_PostsCorrectPayload()
    {
        string? capturedBody = null;
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "*bids")
               .With(req =>
               {
                   capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                   return true;
               })
               .Respond(HttpStatusCode.OK);

        var client = BuildClient(handler);
        var profile = new CustomerProfile { Company = "Acme", Plan = "startup", Name = "Jane", Email = "j@acme.com" };
        var result = await client.SubmitLeadIntakeAsync(profile);

        Assert.True(result);
        Assert.NotNull(capturedBody);
        Assert.Contains("Acme", capturedBody);
        Assert.Contains("fca-mobile-maui", capturedBody);
    }

    [Fact]
    public async Task SubmitLeadIntakeAsync_ReturnsFalse_OnNetworkError()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "*bids").Throw(new HttpRequestException("offline"));

        var client = BuildClient(handler);
        var profile = new CustomerProfile { Company = "X", Plan = "pilot" };
        var result = await client.SubmitLeadIntakeAsync(profile);

        Assert.False(result);
    }
}
