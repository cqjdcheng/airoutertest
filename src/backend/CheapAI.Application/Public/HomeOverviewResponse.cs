namespace CheapAI.Application.Public;

public sealed class HomeOverviewResponse
{
    public string? FeaturedModel { get; init; }

    public IReadOnlyList<HomePopularModelResponse> PopularModels { get; init; } = [];

    public HomeOverviewStatsResponse Stats { get; init; } = new();

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
