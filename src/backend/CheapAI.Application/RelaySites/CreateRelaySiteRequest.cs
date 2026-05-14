namespace CheapAI.Application.RelaySites;

public class CreateRelaySiteRequest
{
    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = string.Empty;

    public string? WebsiteUrl { get; init; }

    public string? Description { get; init; }

    public bool SupportsRefund { get; init; }

    public bool SupportsInvoice { get; init; }

    public bool HasDocs { get; init; }

    public string? DocsUrl { get; init; }

    public string? InviteUrl { get; init; }

    public string? RecentReview { get; init; }

    public IReadOnlyList<RelaySiteOfferUpsertRequest> Offers { get; init; } = [];
}
