using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.Public;

public sealed class PublicTestRecordListItemResponse
{
    public ulong Id { get; init; }

    public string SiteSlug { get; init; } = string.Empty;

    public string SiteName { get; init; } = string.Empty;

    public string? SiteUrl { get; init; }

    public string ModelSlug { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string TestType { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int? FirstTokenMs { get; init; }

    public int? FullResponseMs { get; init; }

    public string? ErrorMessage { get; init; }

    public decimal RiskScore { get; init; }

    public string RiskLevel { get; init; } = "low";

    public DateTime TestedAt { get; init; }
}

public sealed class PublicTestRecordDetailResponse
{
    public ulong Id { get; init; }

    public string SiteSlug { get; init; } = string.Empty;

    public string SiteName { get; init; } = string.Empty;

    public string? SiteUrl { get; init; }

    public string ModelSlug { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string TestType { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int? FirstTokenMs { get; init; }

    public int? FullResponseMs { get; init; }

    public string? ErrorMessage { get; init; }

    public decimal RiskScore { get; init; }

    public string RiskLevel { get; init; } = "low";

    public DateTime TestedAt { get; init; }

    public string ResultSummary { get; init; } = string.Empty;

    public decimal MatchScore { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }

    public int? TotalTokens { get; init; }

    public int EstimatedTokens { get; init; }

    public decimal? TokensPerSecond { get; init; }

    public bool IsStream { get; init; }

    public IReadOnlyList<PublicTestProbeResultResponse> Checks { get; init; } = [];
}

public sealed class PublicTestProbeResultResponse
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Status { get; init; } = "unknown";

    public string Confidence { get; init; } = "medium";

    public decimal ScoreImpact { get; init; }

    public decimal RiskImpact { get; init; }

    public string Evidence { get; init; } = string.Empty;
}

public sealed class PublicRelaySiteRankingItemResponse
{
    public string SiteSlug { get; init; } = string.Empty;

    public string SiteName { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool SupportsInvoice { get; init; }

    public bool SupportsRefund { get; init; }

    public bool HasDocs { get; init; }

    public decimal SiteScore { get; init; }

    public decimal AvailabilityScore { get; init; }

    public decimal StabilityScore { get; init; }

    public decimal RiskScore { get; init; }

    public string RiskLevel { get; init; } = "low";

    public int CoveredModelCount { get; init; }

    public DateTime? LatestTestAt { get; init; }
}

public sealed class PublicModelCatalogItemResponse
{
    public string ModelSlug { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string Vendor { get; init; } = string.Empty;

    public string OfficialModelId { get; init; } = string.Empty;

    public decimal? OfficialInputPriceUsd { get; init; }

    public decimal? OfficialOutputPriceUsd { get; init; }

    public string? CheapestSiteSlug { get; init; }

    public string? CheapestSiteName { get; init; }

    public decimal? EffectiveInputPriceUsd { get; init; }

    public decimal? EffectiveOutputPriceUsd { get; init; }

    public decimal? StabilityScore { get; init; }

    public decimal? RiskScore { get; init; }

    public string RiskLevel { get; init; } = "low";

    public int RelaySiteCount { get; init; }
}

public sealed class PublicCheapestRankingResponse
{
    public string ModelSlug { get; init; } = string.Empty;

    public IReadOnlyList<ModelRankingItemResponse> Items { get; init; } = [];
}

public interface IPublicCatalogRepository
{
    Task<PagedResult<PublicTestRecordListItemResponse>> GetLatestTestsAsync(int page, int pageSize, string? testType = null, CancellationToken cancellationToken = default);

    Task<PublicTestRecordDetailResponse?> GetTestDetailAsync(ulong id, CancellationToken cancellationToken = default);

    Task<PagedResult<PublicRelaySiteRankingItemResponse>> GetRelaySitesAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<PublicModelCatalogItemResponse>> GetModelsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicCheapestRankingResponse>> GetCheapestRankingsAsync(IReadOnlyList<string> modelSlugs, int limit, CancellationToken cancellationToken = default);
}
