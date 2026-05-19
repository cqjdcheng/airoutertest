namespace CheapAI.Application.Operations;

public class TestRecordListItemResponse
{
    public ulong Id { get; init; }

    public string SiteName { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string TestType { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int? FirstTokenMs { get; init; }

    public int? FullResponseMs { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTime TestedAt { get; init; }
}

public sealed class TestRecordDetailResponse : TestRecordListItemResponse
{
    public string? SiteUrl { get; init; }

    public string? ModelSlug { get; init; }

    public decimal RiskScore { get; init; }

    public string RiskLevel { get; init; } = "low";

    public string? ResultSummary { get; init; }

    public decimal MatchScore { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }

    public int? TotalTokens { get; init; }

    public int EstimatedTokens { get; init; }

    public decimal? TokensPerSecond { get; init; }

    public bool IsStream { get; init; }

    public IReadOnlyList<TestProbeResultResponse> Checks { get; init; } = [];
}

public sealed class TestProbeResultResponse
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Confidence { get; init; } = string.Empty;

    public decimal ScoreImpact { get; init; }

    public decimal RiskImpact { get; init; }

    public string Evidence { get; init; } = string.Empty;
}
