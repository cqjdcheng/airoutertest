using CheapAI.Domain.Common;

namespace CheapAI.Domain.RelaySites;

public sealed class RelaySite : AuditableEntity
{
    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string? WebsiteUrl { get; set; }

    public string? Description { get; set; }

    public bool SupportsRefund { get; set; }

    public bool SupportsInvoice { get; set; }

    public bool HasDocs { get; set; }

    public RelaySiteStatus Status { get; set; } = RelaySiteStatus.Draft;
}
