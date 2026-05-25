namespace CheapAI.Application.Public;

public sealed class SiteDetailResponse
{
    public PublicSiteSummaryResponse Site { get; init; } = new();
    public IReadOnlyList<PublicSiteSupportedModelResponse> SupportedModels { get; init; } = [];
    public IReadOnlyList<PublicSitePricingResponse> Pricing { get; init; } = [];
    public IReadOnlyList<PublicSiteLatestTestResponse> LatestTests { get; init; } = [];
    public PublicSiteStatus24hResponse Status24h { get; init; } = new();
    public PublicSiteRiskSummaryResponse RiskSummary { get; init; } = new();
    public PublicSiteTrendsResponse Trends { get; init; } = new();
}

public sealed class PublicSiteSummaryResponse
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
    public string Status { get; init; } = string.Empty;
    public string? InviteUrl { get; init; }
    public string? RecentReview { get; init; }
}

public sealed class PublicSiteSupportedModelResponse
{
    public string ModelSlug { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
}

public sealed class PublicSitePricingResponse
{
    public string ModelSlug { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public decimal? EffectiveInputPriceUsd { get; init; }
    public decimal? EffectiveOutputPriceUsd { get; init; }
}

public sealed class PublicSiteLatestTestResponse
{
    public ulong Id { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string ModelSlug { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public string TestType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int? FirstTokenMs { get; init; }
    public int? FullResponseMs { get; init; }
    public decimal? Availability24h { get; init; }
    public decimal? Stability7d { get; init; }
    public decimal? RiskScore { get; init; }
    public string RiskLevel { get; init; } = "low";
    public decimal? MatchScore { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? TestedAt { get; init; }
}

public sealed class PublicSiteRiskSummaryResponse
{
    public decimal? MaxRiskScore { get; init; }
    public string RiskLevel { get; init; } = "low";
}

public sealed class PublicSiteTrendsResponse
{
    public IReadOnlyList<PublicTrendPointResponse> Price { get; init; } = [];
    public IReadOnlyList<PublicTrendPointResponse> Stability { get; init; } = [];
}

public sealed class PublicTrendPointResponse
{
    public string Label { get; init; } = string.Empty;
    public decimal Value { get; init; }
}
