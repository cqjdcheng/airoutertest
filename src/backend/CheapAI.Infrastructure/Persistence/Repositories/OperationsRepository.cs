using CheapAI.Application.Common.Paging;
using CheapAI.Application.Operations;
using CheapAI.Application.Participation;
using CheapAI.Application.Scoring;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;
using CheapAI.Application.Common.Exceptions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class OperationsRepository(ISqlSugarClient db, ISelfTestRunner selfTestRunner) : IOperationsRepository
{
    public async Task<PagedResult<RelayOfferListItemResponse>> GetOffersAsync(OperationListQuery query, CancellationToken cancellationToken = default)
    {
        RefAsync<int> total = 0;
        var items = await db.Queryable<RelayOfferEntity, RelaySiteEntity, AiModelEntity>(
                (offer, site, model) => new JoinQueryInfos(
                    JoinType.Inner, offer.SiteId == site.Id,
                    JoinType.Inner, offer.ModelId == model.Id))
            .Where((offer, site, model) => site.DeletedAt == null && model.DeletedAt == null)
            .OrderBy((offer, site, model) => offer.UpdatedAt, OrderByType.Desc)
            .Select((offer, site, model) => new RelayOfferListItemResponse
            {
                Id = offer.Id,
                SiteName = site.Name,
                SiteSlug = site.Slug,
                ModelName = model.DisplayName,
                ModelSlug = model.Slug,
                OfficialInputPriceUsd = offer.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = offer.OfficialOutputPriceUsd,
                SiteInputPriceUsd = offer.SiteInputPriceUsd,
                SiteOutputPriceUsd = offer.SiteOutputPriceUsd,
                EffectiveInputPriceUsd = offer.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = offer.EffectiveOutputPriceUsd,
                Status = offer.Status,
                CrawledAt = offer.CrawledAt,
                ReviewedAt = offer.ReviewedAt
            })
            .ToPageListAsync(query.Page, query.PageSize, total, cancellationToken);

        return ToPaged(items, query, total);
    }

    public async Task<PagedResult<TestRecordListItemResponse>> GetTestRecordsAsync(OperationListQuery query, CancellationToken cancellationToken = default)
    {
        RefAsync<int> total = 0;
        var rows = await db.Queryable<TestRecordEntity, RelaySiteEntity, AiModelEntity>(
                (record, site, model) => new JoinQueryInfos(
                    JoinType.Left, record.SiteId == site.Id,
                    JoinType.Left, record.ModelId == model.Id))
            .Where((record, site, model) =>
                (record.SiteId == 0 || site.DeletedAt == null) &&
                (record.ModelId == 0 || model.DeletedAt == null))
            .OrderBy((record, site, model) => record.TestedAt, OrderByType.Desc)
            .Select((record, site, model) => new AdminTestRecordQueryRow
            {
                Id = record.Id,
                SiteName = site.Name,
                FallbackSiteName = record.SiteName,
                FallbackSiteUrl = record.SiteUrl,
                ModelName = model.DisplayName,
                FallbackModelName = record.ModelName,
                FallbackModelSlug = record.ModelSlug,
                TestType = record.TestType,
                Status = record.Status,
                FirstTokenMs = record.FirstTokenMs,
                FullResponseMs = record.FullResponseMs,
                ErrorMessage = record.ErrorMessage,
                TestedAt = record.TestedAt
            })
            .ToPageListAsync(query.Page, query.PageSize, total, cancellationToken);

        var items = rows.Select(MapAdminTestRecord).ToList();
        return ToPaged(items, query, total);
    }

    public async Task<PagedResult<RiskEvidenceListItemResponse>> GetRisksAsync(OperationListQuery query, CancellationToken cancellationToken = default)
    {
        RefAsync<int> total = 0;
        var items = await db.Queryable<RiskEvidenceEntity, RelaySiteEntity, AiModelEntity>(
                (risk, site, model) => new JoinQueryInfos(
                    JoinType.Inner, risk.SiteId == site.Id,
                    JoinType.Inner, risk.ModelId == model.Id))
            .OrderBy((risk, site, model) => risk.RiskScore, OrderByType.Desc)
            .OrderBy((risk, site, model) => risk.CreatedAt, OrderByType.Desc)
            .Select((risk, site, model) => new RiskEvidenceListItemResponse
            {
                Id = risk.Id,
                SiteName = site.Name,
                ModelName = model.DisplayName,
                RuleCode = risk.RuleCode,
                RiskLevel = risk.RiskLevel,
                RiskScore = risk.RiskScore,
                EvidenceSummary = risk.EvidenceSummary,
                ReviewStatus = risk.ReviewStatus,
                CreatedAt = risk.CreatedAt
            })
            .ToPageListAsync(query.Page, query.PageSize, total, cancellationToken);

        return ToPaged(items, query, total);
    }

    public async Task<RiskEvidenceDetailResponse?> GetRiskAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return await db.Queryable<RiskEvidenceEntity, RelaySiteEntity, AiModelEntity>(
                (risk, site, model) => new JoinQueryInfos(
                    JoinType.Inner, risk.SiteId == site.Id,
                    JoinType.Inner, risk.ModelId == model.Id))
            .Where((risk, site, model) => risk.Id == id)
            .Select((risk, site, model) => new RiskEvidenceDetailResponse
            {
                Id = risk.Id,
                SiteName = site.Name,
                ModelName = model.DisplayName,
                TestRecordId = risk.TestRecordId,
                RuleCode = risk.RuleCode,
                RiskLevel = risk.RiskLevel,
                RiskScore = risk.RiskScore,
                EvidenceSummary = risk.EvidenceSummary,
                EvidenceJson = risk.EvidenceJson,
                ReviewStatus = risk.ReviewStatus,
                ReviewedAt = risk.ReviewedAt,
                CreatedAt = risk.CreatedAt,
                UpdatedAt = risk.UpdatedAt
            })
            .FirstAsync(cancellationToken);
    }

    public async Task ReviewRiskAsync(ulong id, string reviewStatus, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var affected = await db.Updateable<RiskEvidenceEntity>()
            .SetColumns(x => new RiskEvidenceEntity
            {
                ReviewStatus = reviewStatus,
                ReviewedAt = now,
                UpdatedAt = now
            })
            .Where(x => x.Id == id)
            .ExecuteCommandAsync(cancellationToken);

        if (affected == 0)
        {
            throw new AppNotFoundException("Risk evidence does not exist.");
        }
    }

    public async Task<PagedResult<JobExecutionLogListItemResponse>> GetJobLogsAsync(OperationListQuery query, CancellationToken cancellationToken = default)
    {
        RefAsync<int> total = 0;
        var items = await db.Queryable<JobExecutionLogEntity>()
            .OrderBy(x => x.CreatedAt, OrderByType.Desc)
            .Select(x => new JobExecutionLogListItemResponse
            {
                Id = x.Id,
                JobCategory = x.JobCategory,
                JobId = x.JobId,
                Status = x.Status,
                Message = x.Message,
                StartedAt = x.StartedAt,
                FinishedAt = x.FinishedAt,
                CreatedAt = x.CreatedAt
            })
            .ToPageListAsync(query.Page, query.PageSize, total, cancellationToken);

        return ToPaged(items, query, total);
    }

    public async Task VerifyOfferAsync(ulong id, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var affected = await db.Updateable<RelayOfferEntity>()
            .SetColumns(x => new RelayOfferEntity
            {
                ReviewedAt = now,
                UpdatedAt = now
            })
            .Where(x => x.Id == id)
            .ExecuteCommandAsync(cancellationToken);

        if (affected == 0)
        {
            throw new AppNotFoundException("Offer does not exist.");
        }
    }

    public async Task<JobActionResponse> RunManualCrawlAsync(ulong? adminUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var jobId = (ulong)await db.Insertable(new CrawlJobEntity
        {
            JobType = "manual_price_crawl",
            Status = "running",
            RequestedBy = adminUserId,
            StartedAt = now,
            CreatedAt = now
        }).ExecuteReturnBigIdentityAsync();

        var sites = await db.Queryable<RelaySiteEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "active")
            .ToListAsync(cancellationToken);
        var models = await db.Queryable<AiModelEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "active")
            .ToListAsync(cancellationToken);

        var affected = 0;
        foreach (var site in sites)
        {
            foreach (var model in models)
            {
                var officialInput = model.OfficialInputPriceUsd ?? 0;
                var officialOutput = model.OfficialOutputPriceUsd ?? 0;
                var siteInput = Math.Round(officialInput * 0.8m, 6);
                var siteOutput = Math.Round(officialOutput * 0.8m, 6);
                var entity = new RelayOfferEntity
                {
                    SiteId = site.Id,
                    ModelId = model.Id,
                    SourceType = "crawl",
                    Currency = "USD",
                    OfficialInputPriceUsd = officialInput,
                    OfficialOutputPriceUsd = officialOutput,
                    SiteInputPriceUsd = siteInput,
                    SiteOutputPriceUsd = siteOutput,
                    RechargeRatio = 1,
                    BonusRatio = 0,
                    EffectiveInputPriceUsd = PriceCalculator.CalculateEffectiveUsd(siteInput, 1, 0),
                    EffectiveOutputPriceUsd = PriceCalculator.CalculateEffectiveUsd(siteOutput, 1, 0),
                    Status = "active",
                    CrawledAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var existing = await db.Queryable<RelayOfferEntity>()
                    .FirstAsync(x => x.SiteId == site.Id && x.ModelId == model.Id && x.SourceType == "crawl", cancellationToken);

                if (existing is null)
                {
                    await db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
                }
                else
                {
                    entity.Id = existing.Id;
                    entity.CreatedAt = existing.CreatedAt;
                    await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
                }

                affected++;
            }
        }

        await MarkCrawlJobSucceededAsync(jobId, affected, now, cancellationToken);
        return await LogActionAsync("crawl", jobId, affected, "价格抓取样本已生成", now, cancellationToken);
    }

    public async Task<JobActionResponse> RunManualTestAsync(ulong? adminUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var jobId = (ulong)await db.Insertable(new TestJobEntity
        {
            JobType = "platform_model_test",
            Status = "running",
            RequestedBy = adminUserId,
            StartedAt = now,
            CreatedAt = now
        }).ExecuteReturnBigIdentityAsync();

        var offers = await db.Queryable<RelayOfferEntity, RelaySiteEntity, AiModelEntity>(
                (offer, site, model) => new JoinQueryInfos(
                    JoinType.Inner, offer.SiteId == site.Id,
                    JoinType.Inner, offer.ModelId == model.Id))
            .Where((offer, site, model) =>
                offer.Status == "active" &&
                site.Status == "active" &&
                site.DeletedAt == null &&
                site.AutoTestEnabled &&
                site.TestApiKey != null &&
                site.TestApiKey != "" &&
                model.DeletedAt == null)
            .Select((offer, site, model) => new PlatformTestOfferRow
            {
                SiteId = offer.SiteId,
                ModelId = offer.ModelId,
                ChannelId = offer.ChannelId,
                SiteUrl = site.BaseUrl,
                SiteName = site.Name,
                TestApiKey = site.TestApiKey!,
                TestIntervalMinutes = site.TestIntervalMinutes,
                LastAutoTestAt = site.LastAutoTestAt,
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                RequestName = model.RequestName,
                OfficialModelId = model.OfficialModelId
            })
            .ToListAsync(cancellationToken);

        var affected = 0;
        foreach (var offer in offers
                     .Where(x => x.LastAutoTestAt is null || x.LastAutoTestAt <= now.AddMinutes(-Math.Max(15, x.TestIntervalMinutes)))
                     .Take(50))
        {
            var request = new CreateSelfTestRequest
            {
                SiteUrl = offer.SiteUrl,
                ModelName = FirstNonEmpty(offer.RequestName, offer.OfficialModelId, offer.ModelName, offer.ModelSlug),
                ApiKey = offer.TestApiKey,
                IsStream = true,
                TestMode = "comprehensive"
            };
            var result = await selfTestRunner.ExecuteAsync(request, cancellationToken);
            var status = NormalizeRecordStatus(result.Status);
            await db.Insertable(new TestRecordEntity
            {
                SiteId = offer.SiteId,
                ModelId = offer.ModelId,
                ChannelId = offer.ChannelId,
                SiteUrl = offer.SiteUrl,
                SiteName = offer.SiteName,
                ModelSlug = offer.ModelSlug,
                ModelName = offer.ModelName,
                TestType = "auto",
                IsStream = true,
                Status = status,
                FirstTokenMs = result.FirstTokenMs,
                FullResponseMs = result.FullResponseMs,
                ErrorCode = status == "success" ? null : "auto_test_failed",
                ErrorMessage = status == "success" ? null : result.ResultSummary,
                PromptHash = HashText($"auto:{request.ModelName}:{request.IsStream}"),
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
                TestedAt = now,
                CreatedAt = now
            }).ExecuteCommandAsync(cancellationToken);

            await db.Updateable<RelaySiteEntity>()
                .SetColumns(x => new RelaySiteEntity
                {
                    LastAutoTestAt = now,
                    UpdatedAt = now
                })
                .Where(x => x.Id == offer.SiteId)
                .ExecuteCommandAsync(cancellationToken);
            affected++;
        }

        await MarkTestJobSucceededAsync(jobId, affected, now, cancellationToken);
        return await LogActionAsync("test", jobId, affected, "自动中转测试已完成", now, cancellationToken);
    }

    public async Task<JobActionResponse> RecalculateRisksAsync(ulong? adminUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var records = await db.Queryable<TestRecordEntity>()
            .OrderBy(x => x.TestedAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        var latestRecords = records
            .GroupBy(x => new { x.SiteId, x.ModelId })
            .Select(x => x.First())
            .ToList();

        var affected = 0;
        foreach (var record in latestRecords)
        {
            if (record.SiteId == 0 || record.ModelId == 0)
            {
                continue;
            }

            var riskScore = record.RiskScore > 0
                ? record.RiskScore
                : RiskScoreCalculator.ResolveRiskScore(record.Status, record.FirstTokenMs, record.FullResponseMs);
            var entity = new RiskEvidenceEntity
            {
                SiteId = record.SiteId,
                ModelId = record.ModelId,
                TestRecordId = record.Id,
                RuleCode = "unified_test_signal",
                RiskLevel = string.IsNullOrWhiteSpace(record.RiskLevel) ? RiskScoreCalculator.ResolveRiskLevel(riskScore) : record.RiskLevel,
                RiskScore = riskScore,
                EvidenceSummary = record.ResultSummary ?? "基于统一测试状态、首 token 耗时和完整响应耗时生成的基础风险证据。",
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
            }
            else
            {
                entity.Id = existing.Id;
                entity.CreatedAt = existing.CreatedAt;
                await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            }

            affected++;
        }

        return await LogActionAsync("risk", null, affected, "基础风险分已重算", now, cancellationToken);
    }

    public async Task<JobActionResponse> RebuildRankingsAsync(ulong? adminUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var jobId = (ulong)await db.Insertable(new AggregateJobEntity
        {
            AggregateType = "ranking_snapshot",
            Status = "running",
            RequestedBy = adminUserId,
            StartedAt = now,
            CreatedAt = now
        }).ExecuteReturnBigIdentityAsync();

        var rows = await BuildRankingRowsAsync(cancellationToken);
        var affected = 0;
        affected += await UpsertRankingTypeAsync("price", rows.OrderBy(x => x.PriceSort).ToList(), now, cancellationToken);
        affected += await UpsertRankingTypeAsync("stability", rows.OrderByDescending(x => x.StabilityScore).ToList(), now, cancellationToken);
        affected += await UpsertRankingTypeAsync("value", rows.OrderByDescending(x => x.FinalScore).ToList(), now, cancellationToken);

        await MarkAggregateJobSucceededAsync(jobId, affected, now, cancellationToken);
        return await LogActionAsync("aggregate", jobId, affected, "排行快照已重建", now, cancellationToken);
    }

    private async Task<IReadOnlyList<RankingBuildRow>> BuildRankingRowsAsync(CancellationToken cancellationToken)
    {
        var offers = await db.Queryable<RelayOfferEntity>()
            .Where(x => x.Status == "active")
            .ToListAsync(cancellationToken);

        var rows = new List<RankingBuildRow>();
        foreach (var offer in offers)
        {
            var tests = await db.Queryable<TestRecordEntity>()
                .Where(x => x.SiteId == offer.SiteId && x.ModelId == offer.ModelId)
                .OrderBy(x => x.TestedAt, OrderByType.Desc)
                .Take(20)
                .ToListAsync(cancellationToken);

            var riskScore = await db.Queryable<RiskEvidenceEntity>()
                .Where(x => x.SiteId == offer.SiteId && x.ModelId == offer.ModelId)
                .MaxAsync(x => x.RiskScore, cancellationToken);

            var successCount = tests.Count(x => x.Status == "success");
            var availability = tests.Count == 0 ? 0 : Math.Round(successCount * 100m / tests.Count, 4);
            var stability = tests.Count == 0 ? 0 : Math.Min(99, Math.Round(90 + availability / 10, 4));
            var speed = tests.Count == 0 ? null : (decimal?)Math.Round(tests.Average(x => x.FirstTokenMs ?? 0), 4);
            var priceSort = (offer.EffectiveInputPriceUsd ?? 999999) + (offer.EffectiveOutputPriceUsd ?? 999999);
            var finalScore = Math.Round(availability * 0.35m + stability * 0.35m + Math.Max(0, 100 - riskScore) * 0.2m + Math.Max(0, 100 - priceSort) * 0.1m, 4);

            rows.Add(new RankingBuildRow
            {
                SiteId = offer.SiteId,
                ModelId = offer.ModelId,
                EffectiveInputPriceUsd = offer.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = offer.EffectiveOutputPriceUsd,
                AvailabilityScore = availability,
                StabilityScore = stability,
                SpeedScore = speed,
                RiskScore = riskScore,
                FinalScore = finalScore,
                PriceSort = priceSort
            });
        }

        return rows;
    }

    private async Task<int> UpsertRankingTypeAsync(string rankingType, IReadOnlyList<RankingBuildRow> rows, DateTime snapshotAt, CancellationToken cancellationToken)
    {
        var affected = 0;
        var grouped = rows.GroupBy(x => x.ModelId);
        foreach (var group in grouped)
        {
            var rank = 1;
            foreach (var row in group)
            {
                var entity = new ModelRankingSnapshotEntity
                {
                    RankingType = rankingType,
                    WindowType = "7d",
                    ModelId = row.ModelId,
                    SiteId = row.SiteId,
                    RankPosition = rank++,
                    EffectiveInputPriceUsd = row.EffectiveInputPriceUsd,
                    EffectiveOutputPriceUsd = row.EffectiveOutputPriceUsd,
                    AvailabilityScore = row.AvailabilityScore,
                    StabilityScore = row.StabilityScore,
                    SpeedScore = row.SpeedScore,
                    RiskScore = row.RiskScore,
                    FinalScore = row.FinalScore,
                    SnapshotAt = snapshotAt
                };

                var existing = await db.Queryable<ModelRankingSnapshotEntity>()
                    .FirstAsync(x =>
                        x.RankingType == rankingType &&
                        x.WindowType == "7d" &&
                        x.ModelId == row.ModelId &&
                        x.SiteId == row.SiteId,
                        cancellationToken);

                if (existing is null)
                {
                    await db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
                }
                else
                {
                    entity.Id = existing.Id;
                    await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
                }

                affected++;
            }
        }

        return affected;
    }

    private async Task MarkCrawlJobSucceededAsync(ulong jobId, int affected, DateTime now, CancellationToken cancellationToken)
    {
        await db.Updateable<CrawlJobEntity>()
            .SetColumns(x => new CrawlJobEntity { Status = "succeeded", FinishedAt = now })
            .Where(x => x.Id == jobId)
            .ExecuteCommandAsync(cancellationToken);
    }

    private async Task MarkTestJobSucceededAsync(ulong jobId, int affected, DateTime now, CancellationToken cancellationToken)
    {
        await db.Updateable<TestJobEntity>()
            .SetColumns(x => new TestJobEntity { Status = "succeeded", FinishedAt = now })
            .Where(x => x.Id == jobId)
            .ExecuteCommandAsync(cancellationToken);
    }

    private async Task MarkAggregateJobSucceededAsync(ulong jobId, int affected, DateTime now, CancellationToken cancellationToken)
    {
        await db.Updateable<AggregateJobEntity>()
            .SetColumns(x => new AggregateJobEntity { Status = "succeeded", FinishedAt = now })
            .Where(x => x.Id == jobId)
            .ExecuteCommandAsync(cancellationToken);
    }

    private async Task<JobActionResponse> LogActionAsync(string category, ulong? jobId, int affected, string message, DateTime startedAt, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await db.Insertable(new JobExecutionLogEntity
        {
            JobCategory = category,
            JobId = jobId,
            Status = "succeeded",
            Message = $"{message}，影响 {affected} 条。",
            StartedAt = startedAt,
            FinishedAt = now,
            CreatedAt = now
        }).ExecuteCommandAsync(cancellationToken);

        return new JobActionResponse
        {
            JobCategory = category,
            JobId = jobId,
            Status = "succeeded",
            AffectedCount = affected,
            Message = message
        };
    }

    private static PagedResult<T> ToPaged<T>(IReadOnlyList<T> items, OperationListQuery query, long total)
    {
        return new PagedResult<T>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            Total = total
        };
    }

    private static TestRecordListItemResponse MapAdminTestRecord(AdminTestRecordQueryRow row)
    {
        return new TestRecordListItemResponse
        {
            Id = row.Id,
            SiteName = FirstNonEmpty(row.SiteName, row.FallbackSiteName, row.FallbackSiteUrl, "未知站点"),
            ModelName = FirstNonEmpty(row.ModelName, row.FallbackModelName, row.FallbackModelSlug, "未知模型"),
            TestType = row.TestType,
            Status = row.Status,
            FirstTokenMs = row.FirstTokenMs,
            FullResponseMs = row.FullResponseMs,
            ErrorMessage = row.ErrorMessage,
            TestedAt = row.TestedAt
        };
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
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

    private sealed class AdminTestRecordQueryRow
    {
        public ulong Id { get; init; }

        public string? SiteName { get; init; }

        public string? FallbackSiteName { get; init; }

        public string? FallbackSiteUrl { get; init; }

        public string? ModelName { get; init; }

        public string? FallbackModelName { get; init; }

        public string? FallbackModelSlug { get; init; }

        public string TestType { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public int? FirstTokenMs { get; init; }

        public int? FullResponseMs { get; init; }

        public string? ErrorMessage { get; init; }

        public DateTime TestedAt { get; init; }
    }

    private sealed class RankingBuildRow
    {
        public ulong SiteId { get; init; }

        public ulong ModelId { get; init; }

        public decimal? EffectiveInputPriceUsd { get; init; }

        public decimal? EffectiveOutputPriceUsd { get; init; }

        public decimal AvailabilityScore { get; init; }

        public decimal StabilityScore { get; init; }

        public decimal? SpeedScore { get; init; }

        public decimal RiskScore { get; init; }

        public decimal FinalScore { get; init; }

        public decimal PriceSort { get; init; }
    }

    private sealed class PlatformTestOfferRow
    {
        public ulong SiteId { get; init; }

        public ulong ModelId { get; init; }

        public ulong? ChannelId { get; init; }

        public string SiteUrl { get; init; } = string.Empty;

        public string SiteName { get; init; } = string.Empty;

        public string TestApiKey { get; init; } = string.Empty;

        public int TestIntervalMinutes { get; init; }

        public DateTime? LastAutoTestAt { get; init; }

        public string ModelSlug { get; init; } = string.Empty;

        public string ModelName { get; init; } = string.Empty;

        public string RequestName { get; init; } = string.Empty;

        public string OfficialModelId { get; init; } = string.Empty;
    }
}
