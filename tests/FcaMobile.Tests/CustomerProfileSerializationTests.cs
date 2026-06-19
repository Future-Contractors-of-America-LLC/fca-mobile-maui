using System.Text.Json;
using Fca.Mobile.Models;

namespace FcaMobile.Tests;

/// <summary>
/// Guards the security guarantee that <see cref="CustomerProfile.Password"/> is
/// never written to persistent storage via JSON serialization.
/// </summary>
public sealed class CustomerProfileSerializationTests
{
    [Fact]
    public void Serialize_DoesNotIncludePassword()
    {
        var profile = new CustomerProfile
        {
            Email = "contractor@acme.com",
            Company = "Acme",
            Plan = "startup",
            Name = "Jane Doe",
            Password = "super-secret-password",
        };

        var json = JsonSerializer.Serialize(profile);

        Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("super-secret-password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_IncludesNonSensitiveFields()
    {
        var profile = new CustomerProfile
        {
            Email = "contractor@acme.com",
            Company = "Acme Builders",
            Plan = "pilot",
            Name = "Jane Doe",
            Password = "ignored",
        };

        var json = JsonSerializer.Serialize(profile);

        Assert.Contains("contractor@acme.com", json);
        Assert.Contains("Acme Builders", json);
        Assert.Contains("pilot", json);
        Assert.Contains("Jane Doe", json);
    }

    [Fact]
    public void Deserialize_DoesNotRestorePassword()
    {
        var json = """{"plan":"startup","company":"Corp","name":"Bob","email":"bob@corp.com","Password":"leaked"}""";
        var profile = JsonSerializer.Deserialize<CustomerProfile>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(profile);
        Assert.Equal("bob@corp.com", profile.Email);
        // Password property is [JsonIgnore] — deserialization must also ignore it
        Assert.Equal("", profile.Password);
    }

    [Fact]
    public void RoundTrip_PreservesAllNonSensitiveFields()
    {
        var original = new CustomerProfile
        {
            Email = "a@b.com",
            Company = "B Corp",
            Plan = "enterprise",
            Name = "Alice",
            Password = "runtime-only",
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<CustomerProfile>(json)!;

        Assert.Equal(original.Email, restored.Email);
        Assert.Equal(original.Company, restored.Company);
        Assert.Equal(original.Plan, restored.Plan);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal("", restored.Password); // Never round-tripped
    }
}
