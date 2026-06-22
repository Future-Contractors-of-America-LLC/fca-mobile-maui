using System.Text.Json.Serialization;

namespace Fca.Mobile.Models;

public sealed class CustomerProfile
{
    public string Plan { get; set; } = "startup";
    public string Company { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string Role { get; set; } = "";
    public string WorkspaceLabel { get; set; } = "";
    public Dictionary<string, bool>? EnabledProducts { get; set; }
    public Dictionary<string, bool>? EnabledComms { get; set; }

    [JsonIgnore]
    public string Password { get; set; } = "";
}

public sealed class BidRecord
{
    public string? Id { get; set; }
    public string? Package { get; set; }
    public string? Value { get; set; }
    public string? Status { get; set; }
    public string? Blocker { get; set; }
    public string? Estimator { get; set; }
    public string? ScopePackage { get; set; }
    public string? DueDate { get; set; }
    public string? NextCommercialMove { get; set; }
    public string? LinkedProjectId { get; set; }

    [JsonIgnore]
    public string DisplayTitle => string.IsNullOrWhiteSpace(Package) ? ScopePackage ?? "Bid package" : Package;
}

public sealed class ProjectRecord
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Customer { get; set; }
    public string? Stage { get; set; }

    [JsonPropertyName("nextAction")]
    public string? NextAction { get; set; }
}

public sealed class FileRecord
{
    [JsonPropertyName("fileId")]
    public string? FileId { get; set; }

    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? Discipline { get; set; }
    public string? VersionLabel { get; set; }
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

public sealed class BillingSummaryRecord
{
    public string? ProjectId { get; set; }
    public string? ContractValue { get; set; }
    public string? BilledToDate { get; set; }
    public string? OutstandingToBill { get; set; }
    public string? CollectionsStatus { get; set; }
}

public sealed class BillingSummarySnapshot
{
    public int Count { get; set; }
    public List<BillingSummaryRecord>? Items { get; set; }
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
    public string? Key { get; set; }
    public string? Title { get; set; }
    public string? Credential { get; set; }
    public string? Audience { get; set; }
    public string? Duration { get; set; }
    public string? Format { get; set; }
    public string? LinkedSurface { get; set; }
}
