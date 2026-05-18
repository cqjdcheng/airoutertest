namespace CheapAI.Application.Public;

public sealed class HomeOverviewResponse
{
    public string? FeaturedModel { get; init; }

    public IReadOnlyList<HomePopularModelResponse> PopularModels { get; init; } = [];

    public HomeOverviewStatsResponse Stats { get; init; } = new();

    public IReadOnlyList<RankingCardResponse> TopPriceCards { get; init; } = [];

    public IReadOnlyList<RankingCardResponse> TopStabilityCards { get; init; } = [];

    public IReadOnlyList<RiskHighlightResponse> RiskHighlights { get; init; } = [];

    public DataPolicySummaryResponse DataPolicySummary { get; init; } = new();
}

public sealed class HomePopularModelResponse
{
    public string ModelSlug { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string RequestName { get; init; } = string.Empty;

    public string ApiType { get; init; } = "openai";
}

public sealed class HomeOverviewStatsResponse
{
    public long SiteCount { get; init; }

    public long ModelCount { get; init; }

    public long TestCount { get; init; }

    public DateTime? LatestTestAt { get; init; }
}

public sealed class RankingCardResponse
{
    public string ModelSlug { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string SiteSlug { get; init; } = string.Empty;

    public string SiteName { get; init; } = string.Empty;

    public decimal? EffectiveInputPriceUsd { get; init; }

    public decimal? EffectiveOutputPriceUsd { get; init; }

    public decimal? StabilityScore { get; init; }

    public decimal? RiskScore { get; init; }
}

public sealed class RiskHighlightResponse
{
    public string SiteSlug { get; init; } = string.Empty;

    public string SiteName { get; init; } = string.Empty;

    public string ModelSlug { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public decimal RiskScore { get; init; }
}

public sealed class DataPolicySummaryResponse
{
    public string PriceRule { get; init; } = "实际折算价为主排序字段";

    public string StabilityRule { get; init; } = "公共排行只使用平台定时测试数据";

    public string RiskRule { get; init; } = "风险分采用规则累加并封顶 100";
}
