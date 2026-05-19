namespace CheapAI.Application.RelaySites;

public sealed class RelaySiteDetailResponse
{
    public ulong Id { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = string.Empty;

    public string? WebsiteUrl { get; init; }

    public string? Description { get; init; }

    public bool SupportsRefund { get; init; }

    public bool SupportsInvoice { get; init; }

    public bool HasDocs { get; init; }

    public string? DocsUrl { get; init; }

    public string Status { get; init; } = RelaySiteStatusValue.Draft;

    public string? InviteUrl { get; init; }

    public string? RecentReview { get; init; }

    public bool AutoTestEnabled { get; init; }

    public bool HasTestApiKey { get; init; }

    public int TestIntervalMinutes { get; init; } = 60;

    public DateTime? LastAutoTestAt { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public IReadOnlyList<RelaySiteOfferResponse> Offers { get; init; } = [];

    public IReadOnlyList<RelaySiteTestRecordResponse> RecentTests { get; init; } = [];
}
