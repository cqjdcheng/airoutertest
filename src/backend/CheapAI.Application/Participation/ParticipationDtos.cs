using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.Participation;

public sealed class CreateSelfTestRequest
{
    public string SiteUrl { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public bool IsStream { get; init; } = true;

    public string TestMode { get; init; } = "basic";

    public string ChallengeId { get; init; } = string.Empty;

    public string ChallengeAnswer { get; init; } = string.Empty;
}

public sealed class SelfTestResponse
{
    public string Id { get; init; } = string.Empty;

    public ulong? TestRecordId { get; init; }

    public string TestType { get; init; } = "user";

    public string SiteSlug { get; init; } = string.Empty;

    public string SiteName { get; init; } = string.Empty;

    public string ModelSlug { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int? FirstTokenMs { get; init; }

    public int? FullResponseMs { get; init; }

    public decimal RiskScore { get; init; }

    public string RiskLevel { get; init; } = string.Empty;

    public string ResultSummary { get; init; } = string.Empty;

    public decimal MatchScore { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }

    public int? TotalTokens { get; init; }

    public int EstimatedTokens { get; init; }

    public decimal? TokensPerSecond { get; init; }

    public IReadOnlyList<SelfTestProbeResult> Checks { get; init; } = [];

    public DateTime CreatedAt { get; init; }
}

public sealed class SelfTestExecutionResult
{
    public string Status { get; init; } = "failed";

    public int? FirstTokenMs { get; init; }

    public int? FullResponseMs { get; init; }

    public decimal RiskScore { get; init; }

    public string RiskLevel { get; init; } = "medium";

    public string ResultSummary { get; init; } = string.Empty;

    public decimal MatchScore { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }

    public int? TotalTokens { get; init; }

    public int EstimatedTokens { get; init; }

    public decimal? TokensPerSecond { get; init; }

    public IReadOnlyList<SelfTestProbeResult> Checks { get; init; } = [];
}

public sealed class SelfTestProbeResult
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

public sealed class SelfTestChallengeResponse
{
    public string Id { get; init; } = string.Empty;

    public string Question { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }
}

public sealed class CreateSiteSubmissionRequest
{
    public string SiteName { get; init; } = string.Empty;

    public string SiteUrl { get; init; } = string.Empty;

    public string? Contact { get; init; }

    public string? Description { get; init; }
}

public sealed class SiteSubmissionListItemResponse
{
    public ulong Id { get; init; }

    public string SiteName { get; init; } = string.Empty;

    public string SiteUrl { get; init; } = string.Empty;

    public string? Contact { get; init; }

    public string? Description { get; init; }

    public string ReviewStatus { get; init; } = string.Empty;

    public string? ReviewNote { get; init; }

    public DateTime CreatedAt { get; init; }
}

public sealed class ReviewSubmissionRequest
{
    public string? ReviewNote { get; init; }
}

public sealed class CapabilityRankingItemResponse
{
    public string ModelSlug { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public int RankPosition { get; init; }

    public decimal CapabilityScore { get; init; }

    public DateTime SnapshotAt { get; init; }
}

public class ArticleListItemResponse
{
    public ulong Id { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Summary { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime? PublishedAt { get; init; }

    public IReadOnlyList<ArticleTagResponse> Tags { get; init; } = [];
}

public sealed class ArticleDetailResponse : ArticleListItemResponse
{
    public string ContentMd { get; init; } = string.Empty;
}

public class CreateArticleRequest
{
    public string Slug { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Summary { get; init; }

    public string ContentMd { get; init; } = string.Empty;

    public string Status { get; init; } = "draft";

    public IReadOnlyList<ulong> TagIds { get; init; } = [];
}

public sealed class UpdateArticleRequest : CreateArticleRequest;

public sealed class ArticleTagResponse
{
    public ulong Id { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

public sealed class UpsertArticleTagRequest
{
    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; } = 1000;
}

public interface IParticipationRepository
{
    Task<SelfTestResponse> CreateSelfTestAsync(CreateSelfTestRequest request, SelfTestExecutionResult result, CancellationToken cancellationToken = default);

    Task<SelfTestResponse?> GetSelfTestAsync(string id, CancellationToken cancellationToken = default);

    Task<ulong> CreateSubmissionAsync(CreateSiteSubmissionRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<SiteSubmissionListItemResponse>> GetSubmissionsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task ReviewSubmissionAsync(ulong id, string status, string? reviewNote, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CapabilityRankingItemResponse>> GetCapabilityRankingAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<ArticleListItemResponse>> GetArticlesAsync(int page, int pageSize, bool publicOnly, string? tagSlug = null, CancellationToken cancellationToken = default);

    Task<ArticleDetailResponse?> GetArticleBySlugAsync(string slug, bool publicOnly, CancellationToken cancellationToken = default);

    Task<ArticleDetailResponse?> GetArticleByIdAsync(ulong id, CancellationToken cancellationToken = default);

    Task<ulong> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default);

    Task UpdateArticleAsync(ulong id, UpdateArticleRequest request, CancellationToken cancellationToken = default);

    Task UpdateArticleStatusAsync(ulong id, string status, CancellationToken cancellationToken = default);

    Task DeleteArticleAsync(ulong id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArticleTagResponse>> GetArticleTagsAsync(CancellationToken cancellationToken = default);

    Task<ulong> CreateArticleTagAsync(UpsertArticleTagRequest request, CancellationToken cancellationToken = default);

    Task UpdateArticleTagAsync(ulong id, UpsertArticleTagRequest request, CancellationToken cancellationToken = default);

    Task DeleteArticleTagAsync(ulong id, CancellationToken cancellationToken = default);
}

public interface ISelfTestRunner
{
    Task<SelfTestExecutionResult> ExecuteAsync(CreateSelfTestRequest request, CancellationToken cancellationToken = default);
}

public interface ISelfTestChallengeService
{
    SelfTestChallengeResponse CreateChallenge();

    Task<bool> VerifyAndConsumeAsync(string challengeId, string challengeAnswer, CancellationToken cancellationToken = default);
}
