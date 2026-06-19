using System.Text.Json.Serialization;

namespace Fca.Mobile.Models;

public sealed class CustomerProfile
{
    public string Plan { get; set; } = "startup";
    public string Company { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";

    [JsonIgnore]
    public string Password { get; set; } = "";
}

public sealed class BidRecord
{
    public string? Id { get; set; }
    public string? Company { get; set; }
    public string? ProjectName { get; set; }
    public string? Status { get; set; }
    public decimal? Value { get; set; }
}

public sealed class ProjectRecord
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Stage { get; set; }
    public string? NextStep { get; set; }
}

public sealed class FileRecord
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
}

public sealed class PortalMessage
{
    public string? Id { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public string? Channel { get; set; }
}

public sealed class PortalInvoice
{
    public string? Id { get; set; }
    public string? InvoiceName { get; set; }
    public string? Amount { get; set; }
    public string? Status { get; set; }
}

public sealed class SupportTicket
{
    public string? Id { get; set; }
    public string? Subject { get; set; }
    public string? Priority { get; set; }
    public string? Detail { get; set; }
    public string? Status { get; set; }
}

public sealed class AcademySnapshot
{
    public AcademyCatalog? Catalog { get; set; }
}

public sealed class AcademyCatalog
{
    public List<AcademyProgram>? Programs { get; set; }
}

public sealed class AcademyProgram
{
    public string? Title { get; set; }
    public string? Level { get; set; }
    public int? LevelNumber { get; set; }
    public string? Status { get; set; }
    public string? Pathway { get; set; }
}
