using CheapAI.Application.Common.Paging;
using CheapAI.Application.Participation;
using CheapAI.Application.RelaySites;
using CheapAI.Infrastructure.Persistence.Entities;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SqlSugar;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class ParticipationRepository(ISqlSugarClient db, IOptions<MySqlOptions> mySqlOptions) : IParticipationRepository
{
    public async Task<SelfTestResponse> CreateSelfTestAsync(CreateSelfTestRequest request, SelfTestExecutionResult result, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = new SelfTestEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            SiteUrl = request.SiteUrl,
            ModelName = request.ModelName,
            TestMode = request.TestMode,
            IsStream = request.IsStream,
            Status = result.Status,
            FirstTokenMs = result.FirstTokenMs,
            FullResponseMs = result.FullResponseMs,
            RiskScore = result.RiskScore,
            RiskLevel = result.RiskLevel,
            ResultSummary = result.ResultSummary,
            MatchScore = result.MatchScore,
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            TotalTokens = result.TotalTokens,
            EstimatedTokens = result.EstimatedTokens,
            TokensPerSecond = result.TokensPerSecond,
            ChecksJson = JsonSerializer.Serialize(result.Checks),
            CreatedAt = now,
            ExpiresAt = now.AddHours(24)
        };

        await db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
        var testRecord = await CreateUnifiedTestRecordAsync(entity, request, result, now, cancellationToken);
        return MapSelfTest(entity, testRecord);
    }

    public async Task<SelfTestResponse?> GetSelfTestAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(mySqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              id,
              status,
              first_token_ms,
              full_response_ms,
              risk_score,
              risk_level,
              result_summary,
              match_score,
              input_tokens,
              output_tokens,
              total_tokens,
              estimated_tokens,
              tokens_per_second,
              checks_json,
              created_at
            FROM self_tests
            WHERE id = @id AND expires_at > @now
            LIMIT 1
            """;
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@now", DateTime.UtcNow);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var firstTokenOrdinal = reader.GetOrdinal("first_token_ms");
        var fullResponseOrdinal = reader.GetOrdinal("full_response_ms");
        var inputTokensOrdinal = reader.GetOrdinal("input_tokens");
        var outputTokensOrdinal = reader.GetOrdinal("output_tokens");
        var totalTokensOrdinal = reader.GetOrdinal("total_tokens");
        var tokensPerSecondOrdinal = reader.GetOrdinal("tokens_per_second");
        var checksJsonOrdinal = reader.GetOrdinal("checks_json");

        return new SelfTestResponse
        {
            Id = reader.GetGuid("id").ToString("D"),
            Status = reader.GetString("status"),
            FirstTokenMs = await reader.IsDBNullAsync(firstTokenOrdinal, cancellationToken) ? null : reader.GetInt32(firstTokenOrdinal),
            FullResponseMs = await reader.IsDBNullAsync(fullResponseOrdinal, cancellationToken) ? null : reader.GetInt32(fullResponseOrdinal),
            RiskScore = reader.GetDecimal("risk_score"),
            RiskLevel = reader.GetString("risk_level"),
            ResultSummary = reader.GetString("result_summary"),
            MatchScore = reader.GetDecimal("match_score"),
            InputTokens = await reader.IsDBNullAsync(inputTokensOrdinal, cancellationToken) ? null : reader.GetInt32(inputTokensOrdinal),
            OutputTokens = await reader.IsDBNullAsync(outputTokensOrdinal, cancellationToken) ? null : reader.GetInt32(outputTokensOrdinal),
            TotalTokens = await reader.IsDBNullAsync(totalTokensOrdinal, cancellationToken) ? null : reader.GetInt32(totalTokensOrdinal),
            EstimatedTokens = reader.GetInt32("estimated_tokens"),
            TokensPerSecond = await reader.IsDBNullAsync(tokensPerSecondOrdinal, cancellationToken) ? null : reader.GetDecimal(tokensPerSecondOrdinal),
            Checks = await reader.IsDBNullAsync(checksJsonOrdinal, cancellationToken)
                ? []
                : JsonSerializer.Deserialize<IReadOnlyList<SelfTestProbeResult>>(reader.GetString(checksJsonOrdinal)) ?? [],
            CreatedAt = reader.GetDateTime("created_at")
        };
    }

    public async Task<ulong> CreateSubmissionAsync(CreateSiteSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return (ulong)await db.Insertable(new SiteSubmissionEntity
        {
            SiteName = request.SiteName,
            SiteUrl = request.SiteUrl,
            Contact = request.Contact,
            Description = request.Description,
            ReviewStatus = "pending",
            CreatedAt = now,
            UpdatedAt = now
        }).ExecuteReturnBigIdentityAsync();
    }

    public async Task<PagedResult<SiteSubmissionListItemResponse>> GetSubmissionsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        RefAsync<int> total = 0;
        var items = await db.Queryable<SiteSubmissionEntity>()
            .OrderBy(x => x.CreatedAt, OrderByType.Desc)
            .Select(x => new SiteSubmissionListItemResponse
            {
                Id = x.Id,
                SiteName = x.SiteName,
                SiteUrl = x.SiteUrl,
                Contact = x.Contact,
                Description = x.Description,
                ReviewStatus = x.ReviewStatus,
                ReviewNote = x.ReviewNote,
                CreatedAt = x.CreatedAt
            })
            .ToPageListAsync(page, pageSize, total, cancellationToken);

        return ToPaged(items, page, pageSize, total);
    }

    public Task ReviewSubmissionAsync(ulong id, string status, string? reviewNote, CancellationToken cancellationToken = default)
    {
        return db.Updateable<SiteSubmissionEntity>()
            .SetColumns(x => new SiteSubmissionEntity
            {
                ReviewStatus = status,
                ReviewNote = reviewNote,
                ReviewedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .Where(x => x.Id == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CapabilityRankingItemResponse>> GetCapabilityRankingAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = await db.Queryable<ModelCapabilitySnapshotEntity, AiModelEntity>(
                (snapshot, model) => snapshot.ModelId == model.Id)
            .Where((snapshot, model) => model.DeletedAt == null && model.Status == "active")
            .OrderBy((snapshot, model) => snapshot.RankPosition, OrderByType.Asc)
            .Select((snapshot, model) => new CapabilityRankingItemResponse
            {
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                Source = snapshot.Source,
                RankPosition = snapshot.RankPosition,
                CapabilityScore = snapshot.CapabilityScore,
                SnapshotAt = snapshot.SnapshotAt
            })
            .ToListAsync(cancellationToken);

        if (snapshots.Count > 0)
        {
            return snapshots;
        }

        var models = await db.Queryable<AiModelEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "active")
            .OrderBy(x => x.CapabilityScore, OrderByType.Desc)
            .OrderBy(x => x.SortOrder, OrderByType.Asc)
            .OrderBy(x => x.Id, OrderByType.Desc)
            .Select(x => new CapabilityFallbackRow
            {
                ModelSlug = x.Slug,
                ModelName = x.DisplayName,
                Source = x.CapabilitySource,
                CapabilityScore = x.CapabilityScore,
                SnapshotAt = x.CapabilityUpdatedAt
            })
            .ToListAsync(cancellationToken);

        return models.Select((model, index) => new CapabilityRankingItemResponse
        {
            ModelSlug = model.ModelSlug,
            ModelName = model.ModelName,
            Source = string.IsNullOrWhiteSpace(model.Source) ? "模型目录" : model.Source,
            RankPosition = index + 1,
            CapabilityScore = model.CapabilityScore ?? 0,
            SnapshotAt = model.SnapshotAt ?? DateTime.UtcNow
        }).ToList();
    }

    public async Task<PagedResult<ArticleListItemResponse>> GetArticlesAsync(int page, int pageSize, bool publicOnly, CancellationToken cancellationToken = default)
    {
        var query = db.Queryable<ArticleEntity>();
        if (publicOnly)
        {
            query = query.Where(x => x.Status == "published");
        }

        RefAsync<int> total = 0;
        var items = await query
            .OrderBy(x => x.PublishedAt, OrderByType.Desc)
            .OrderBy(x => x.CreatedAt, OrderByType.Desc)
            .Select(x => new ArticleListItemResponse
            {
                Id = x.Id,
                Slug = x.Slug,
                Title = x.Title,
                Summary = x.Summary,
                Status = x.Status,
                PublishedAt = x.PublishedAt
            })
            .ToPageListAsync(page, pageSize, total, cancellationToken);

        return ToPaged(items, page, pageSize, total);
    }

    public async Task<ArticleDetailResponse?> GetArticleBySlugAsync(string slug, bool publicOnly, CancellationToken cancellationToken = default)
    {
        var query = db.Queryable<ArticleEntity>().Where(x => x.Slug == slug);
        if (publicOnly)
        {
            query = query.Where(x => x.Status == "published");
        }

        var entity = await query.FirstAsync(cancellationToken);
        return entity is null ? null : MapArticleDetail(entity);
    }

    public async Task<ArticleDetailResponse?> GetArticleByIdAsync(ulong id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Queryable<ArticleEntity>().FirstAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapArticleDetail(entity);
    }

    public async Task<ulong> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return (ulong)await db.Insertable(new ArticleEntity
        {
            Slug = request.Slug,
            Title = request.Title,
            Summary = request.Summary,
            ContentMd = request.ContentMd,
            Status = request.Status,
            PublishedAt = request.Status == "published" ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        }).ExecuteReturnBigIdentityAsync();
    }

    public Task UpdateArticleAsync(ulong id, UpdateArticleRequest request, CancellationToken cancellationToken = default)
    {
        return db.Updateable<ArticleEntity>()
            .SetColumns(x => new ArticleEntity
            {
                Slug = request.Slug,
                Title = request.Title,
                Summary = request.Summary,
                ContentMd = request.ContentMd,
                Status = request.Status,
                PublishedAt = request.Status == "published" ? DateTime.UtcNow : null,
                UpdatedAt = DateTime.UtcNow
            })
            .Where(x => x.Id == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    public Task UpdateArticleStatusAsync(ulong id, string status, CancellationToken cancellationToken = default)
    {
        return db.Updateable<ArticleEntity>()
            .SetColumns(x => new ArticleEntity
            {
                Status = status,
                PublishedAt = status == "published" ? DateTime.UtcNow : null,
                UpdatedAt = DateTime.UtcNow
            })
            .Where(x => x.Id == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    private async Task<TestRecordEntity> CreateUnifiedTestRecordAsync(
        SelfTestEntity selfTest,
        CreateSelfTestRequest request,
        SelfTestExecutionResult result,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var normalizedSiteUrl = NormalizeEndpointUrl(request.SiteUrl);
        var site = await db.Queryable<RelaySiteEntity>()
            .FirstAsync(x => x.DeletedAt == null && x.BaseUrl == normalizedSiteUrl, cancellationToken);
        var model = await db.Queryable<AiModelEntity>()
            .FirstAsync(x =>
                    x.DeletedAt == null &&
                    (x.Slug == request.ModelName ||
                     x.OfficialModelId == request.ModelName ||
                     x.RequestName == request.ModelName ||
                     x.DisplayName == request.ModelName),
                cancellationToken);

        var record = new TestRecordEntity
        {
            SiteId = site?.Id ?? 0,
            ModelId = model?.Id ?? 0,
            SiteUrl = normalizedSiteUrl,
            SiteName = site?.Name ?? HostFromUrl(normalizedSiteUrl),
            ModelSlug = model?.Slug ?? SlugHelper.Normalize(null, request.ModelName),
            ModelName = model?.DisplayName ?? request.ModelName,
            TestType = "user",
            IsStream = request.IsStream,
            Status = NormalizeRecordStatus(result.Status),
            FirstTokenMs = result.FirstTokenMs,
            FullResponseMs = result.FullResponseMs,
            ErrorCode = NormalizeRecordStatus(result.Status) == "success" ? null : "self_test_failed",
            ErrorMessage = NormalizeRecordStatus(result.Status) == "success" ? null : result.ResultSummary,
            PromptHash = HashText($"{request.TestMode}:{request.ModelName}:{request.IsStream}"),
            ResponseHash = HashText(JsonSerializer.Serialize(result.Checks)),
            DetectedModelId = null,
            RiskScore = result.RiskScore,
            RiskLevel = result.RiskLevel,
            ResultSummary = result.ResultSummary,
            MatchScore = result.MatchScore,
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            TotalTokens = result.TotalTokens,
            EstimatedTokens = result.EstimatedTokens,
            TokensPerSecond = result.TokensPerSecond,
            ChecksJson = JsonSerializer.Serialize(result.Checks),
            SelfTestId = selfTest.Id,
            TestedAt = now,
            CreatedAt = now
        };

        record.Id = (ulong)await db.Insertable(record).ExecuteReturnBigIdentityAsync();

        if (record.SiteId > 0 && record.ModelId > 0)
        {
            await UpsertUnifiedRiskEvidenceAsync(record, now, cancellationToken);
        }

        return record;
    }

    private async Task UpsertUnifiedRiskEvidenceAsync(TestRecordEntity record, DateTime now, CancellationToken cancellationToken)
    {
        var entity = new RiskEvidenceEntity
        {
            SiteId = record.SiteId,
            ModelId = record.ModelId,
            TestRecordId = record.Id,
            RuleCode = "unified_test_signal",
            RiskLevel = record.RiskLevel,
            RiskScore = record.RiskScore,
            EvidenceSummary = record.ResultSummary ?? "统一测试记录生成的风险信号。",
            EvidenceJson = JsonSerializer.Serialize(new
            {
                record.Status,
                record.TestType,
                record.FirstTokenMs,
                record.FullResponseMs,
                record.MatchScore,
                record.InputTokens,
                record.OutputTokens,
                record.TotalTokens
            }),
            ReviewStatus = "pending",
            CreatedAt = now,
            UpdatedAt = now
        };

        var existing = await db.Queryable<RiskEvidenceEntity>()
            .FirstAsync(x => x.SiteId == record.SiteId && x.ModelId == record.ModelId && x.RuleCode == entity.RuleCode, cancellationToken);

        if (existing is null)
        {
            await db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
            return;
        }

        entity.Id = existing.Id;
        entity.CreatedAt = existing.CreatedAt;
        await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
    }

    private static SelfTestResponse MapSelfTest(SelfTestEntity entity, TestRecordEntity? testRecord = null)
    {
        return new SelfTestResponse
        {
            Id = entity.Id,
            TestRecordId = testRecord?.Id,
            TestType = testRecord?.TestType ?? "user",
            SiteSlug = testRecord?.SiteId > 0 ? string.Empty : string.Empty,
            SiteName = testRecord?.SiteName ?? entity.SiteUrl,
            ModelSlug = testRecord?.ModelSlug ?? SlugHelper.Normalize(null, entity.ModelName),
            ModelName = testRecord?.ModelName ?? entity.ModelName,
            Status = entity.Status,
            FirstTokenMs = entity.FirstTokenMs,
            FullResponseMs = entity.FullResponseMs,
            RiskScore = entity.RiskScore,
            RiskLevel = entity.RiskLevel,
            ResultSummary = entity.ResultSummary,
            MatchScore = entity.MatchScore,
            InputTokens = entity.InputTokens,
            OutputTokens = entity.OutputTokens,
            TotalTokens = entity.TotalTokens,
            EstimatedTokens = entity.EstimatedTokens,
            TokensPerSecond = entity.TokensPerSecond,
            Checks = string.IsNullOrWhiteSpace(entity.ChecksJson)
                ? []
                : JsonSerializer.Deserialize<IReadOnlyList<SelfTestProbeResult>>(entity.ChecksJson) ?? [],
            CreatedAt = entity.CreatedAt
        };
    }

    private static string NormalizeEndpointUrl(string siteUrl)
    {
        return siteUrl.Trim().TrimEnd('/');
    }

    private static string HostFromUrl(string siteUrl)
    {
        return Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri) ? uri.Host : siteUrl;
    }

    private static string NormalizeRecordStatus(string status)
    {
        return status.Equals("succeeded", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("success", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("pass", StringComparison.OrdinalIgnoreCase)
            ? "success"
            : "failed";
    }

    private static string HashText(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static ArticleDetailResponse MapArticleDetail(ArticleEntity entity)
    {
        return new ArticleDetailResponse
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Title = entity.Title,
            Summary = entity.Summary,
            ContentMd = entity.ContentMd,
            Status = entity.Status,
            PublishedAt = entity.PublishedAt
        };
    }

    private static PagedResult<T> ToPaged<T>(IReadOnlyList<T> items, int page, int pageSize, long total)
    {
        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    private sealed class CapabilityFallbackRow
    {
        public string ModelSlug { get; init; } = string.Empty;
        public string ModelName { get; init; } = string.Empty;
        public string? Source { get; init; }
        public decimal? CapabilityScore { get; init; }
        public DateTime? SnapshotAt { get; init; }
    }
}
