using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.Public;

public sealed class ModelRankingResponse
{
    public RankingModelSummary Model { get; init; } = new();

    public string RankingType { get; init; } = "stability";

    public string Window { get; init; } = "7d";

    public DateTime? SnapshotAt { get; init; }

    public PagedResult<ModelRankingItemResponse> Result { get; init; } = new();
}

public sealed class RankingModelSummary
{
    public string Slug { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;
}

public sealed class ModelRankingItemResponse
{
    public string SiteSlug { get; init; } = string.Empty;

    public string SiteName { get; init; } = string.Empty;

    public decimal? EffectiveInputPriceUsd { get; init; }

    public decimal? EffectiveOutputPriceUsd { get; init; }

    public decimal? Availability24h { get; init; }

    public decimal? Stability7d { get; init; }

    public int? FirstTokenMs { get; init; }

    public int? FullResponseMs { get; init; }

    public decimal? RiskScore { get; init; }

    public string RiskLevel { get; init; } = "low";

    public bool SupportsInvoice { get; init; }

    public bool SupportsRefund { get; init; }

    public bool HasDocs { get; init; }
}
