using CheapAI.Application.Common.Paging;
using CheapAI.Application.Public;
using CheapAI.Application.RelaySites;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;
using System.Text.Json;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class PublicCatalogRepository(ISqlSugarClient db) : IPublicCatalogRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<PagedResult<PublicTestRecordListItemResponse>> GetLatestTestsAsync(int page, int pageSize, string? testType = null, CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 200);
        var query = db.Queryable<TestRecordEntity, RelaySiteEntity, AiModelEntity>(
                (record, site, model) => new JoinQueryInfos(
                    JoinType.Left, record.SiteId == site.Id,
                    JoinType.Left, record.ModelId == model.Id))
            .Where((record, site, model) =>
                (record.SiteId == 0 || site.DeletedAt == null) &&
                (record.ModelId == 0 || model.DeletedAt == null));

        if (!string.IsNullOrWhiteSpace(testType))
        {
            var normalizedType = testType.Trim();
            query = query.Where((record, site, model) => record.TestType == normalizedType);
        }

        RefAsync<int> total = 0;
        var rows = await query
            .OrderBy((record, site, model) => record.TestedAt, OrderByType.Desc)
            .Select((record, site, model) => new TestRecordQueryRow
            {
                Id = record.Id,
                SiteSlug = site.Slug,
                SiteName = site.Name,
                SiteUrl = site.BaseUrl,
                FallbackSiteName = record.SiteName,
                FallbackSiteUrl = record.SiteUrl,
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                FallbackModelSlug = record.ModelSlug,
                FallbackModelName = record.ModelName,
                TestType = record.TestType,
                Status = record.Status,
                FirstTokenMs = record.FirstTokenMs,
                FullResponseMs = record.FullResponseMs,
                ErrorMessage = record.ErrorMessage,
                RiskScore = record.RiskScore,
                RiskLevel = record.RiskLevel,
                ResultSummary = record.ResultSummary,
                MatchScore = record.MatchScore,
                InputTokens = record.InputTokens,
                OutputTokens = record.OutputTokens,
                TotalTokens = record.TotalTokens,
                EstimatedTokens = record.EstimatedTokens,
                TokensPerSecond = record.TokensPerSecond,
                IsStream = record.IsStream,
                ChecksJson = record.ChecksJson,
                TestedAt = record.TestedAt
            })
            .ToPageListAsync(normalizedPage, normalizedPageSize, total, cancellationToken);

        var items = rows.Select(MapListItem).ToList();

        var riskMap = await LoadRiskMapAsync(items.Select(x => (x.SiteSlug, x.ModelSlug)).Distinct().ToList(), cancellationToken);
        items = items.Select(row =>
        {
            var risk = riskMap.GetValueOrDefault($"{row.SiteSlug}|{row.ModelSlug}");
            return row.RiskScore > 0 ? row : ApplyRisk(row, risk);
        }).ToList();

        return ToPaged(items, normalizedPage, normalizedPageSize, total);
    }

    public async Task<PublicTestRecordDetailResponse?> GetTestDetailAsync(ulong id, CancellationToken cancellationToken = default)
    {
        var row = await db.Queryable<TestRecordEntity, RelaySiteEntity, AiModelEntity>(
                (record, site, model) => new JoinQueryInfos(
                    JoinType.Left, record.SiteId == site.Id,
                    JoinType.Left, record.ModelId == model.Id))
            .Where((record, site, model) => record.Id == id)
            .Select((record, site, model) => new TestRecordQueryRow
            {
                Id = record.Id,
                SiteSlug = site.Slug,
                SiteName = site.Name,
                SiteUrl = site.BaseUrl,
                FallbackSiteName = record.SiteName,
                FallbackSiteUrl = record.SiteUrl,
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                FallbackModelSlug = record.ModelSlug,
                FallbackModelName = record.ModelName,
                TestType = record.TestType,
                Status = record.Status,
                FirstTokenMs = record.FirstTokenMs,
                FullResponseMs = record.FullResponseMs,
                ErrorMessage = record.ErrorMessage,
                RiskScore = record.RiskScore,
                RiskLevel = record.RiskLevel,
                ResultSummary = record.ResultSummary,
                MatchScore = record.MatchScore,
                InputTokens = record.InputTokens,
                OutputTokens = record.OutputTokens,
                TotalTokens = record.TotalTokens,
                EstimatedTokens = record.EstimatedTokens,
                TokensPerSecond = record.TokensPerSecond,
                IsStream = record.IsStream,
                ChecksJson = record.ChecksJson,
                TestedAt = record.TestedAt
            })
            .FirstAsync(cancellationToken);

        return row is null ? null : MapDetail(row);
    }

    public async Task<PublicTestRecordDetailResponse?> GetTestDetailByPublicIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (ulong.TryParse(id, out var recordId))
        {
            var recordDetail = await GetTestDetailAsync(recordId, cancellationToken);
            if (recordDetail is not null)
            {
                return recordDetail;
            }
        }

        var selfTest = await db.Queryable<SelfTestEntity>()
            .FirstAsync(x => x.Id == id && x.ExpiresAt > DateTime.UtcNow, cancellationToken);

        return selfTest is null ? null : MapSelfTestDetail(selfTest);
    }

    public async Task<PagedResult<PublicRelaySiteRankingItemResponse>> GetRelaySitesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var sites = await db.Queryable<RelaySiteEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "active")
            .ToListAsync(cancellationToken);
        var siteIds = sites.Select(x => x.Id).ToList();

        List<ModelRankingSnapshotEntity> snapshots = siteIds.Count == 0
            ? []
            : await db.Queryable<ModelRankingSnapshotEntity>()
                .Where(x => siteIds.Contains(x.SiteId) && x.RankingType == "value" && x.WindowType == "7d")
                .ToListAsync(cancellationToken);

        List<SiteModelCoverageRow> offers = siteIds.Count == 0
            ? []
            : await db.Queryable<RelayOfferEntity>()
                .Where(x => siteIds.Contains(x.SiteId) && x.Status == "active")
                .Select(x => new SiteModelCoverageRow
                {
                    SiteId = x.SiteId,
                    ModelId = x.ModelId
                })
                .ToListAsync(cancellationToken);

        var snapshotsBySiteId = snapshots
            .GroupBy(x => x.SiteId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var offerModelIdsBySiteId = offers
            .GroupBy(x => x.SiteId)
            .ToDictionary(x => x.Key, x => x.Select(row => row.ModelId).Distinct().ToList());

        var scored = sites.Select(site =>
        {
            var siteSnapshots = snapshotsBySiteId.GetValueOrDefault(site.Id) ?? [];
            var availability = Average(siteSnapshots.Select(x => x.AvailabilityScore));
            var stability = Average(siteSnapshots.Select(x => x.StabilityScore));
            var risk = siteSnapshots.Count == 0 ? 0m : siteSnapshots.Max(x => x.RiskScore ?? 0m);
            var enterpriseScore = (site.SupportsInvoice ? 34m : 0m) + (site.SupportsRefund ? 33m : 0m) + (site.HasDocs ? 33m : 0m);
            var score = Math.Round(availability * 0.35m + stability * 0.25m + Math.Max(0, 100 - risk) * 0.25m + enterpriseScore * 0.15m, 2);
            var offerModelIds = offerModelIdsBySiteId.GetValueOrDefault(site.Id) ?? [];

            return new SiteRankingRow
            {
                SiteId = site.Id,
                Item = new PublicRelaySiteRankingItemResponse
                {
                    SiteSlug = site.Slug,
                    SiteName = site.Name,
                    Description = site.Description,
                    SupportsInvoice = site.SupportsInvoice,
                    SupportsRefund = site.SupportsRefund,
                    HasDocs = site.HasDocs,
                    SiteScore = score,
                    AvailabilityScore = availability,
                    StabilityScore = stability,
                    RiskScore = risk,
                    RiskLevel = ResolveRiskLevel(risk),
                    CoveredModelCount = siteSnapshots.Select(x => x.ModelId)
                        .Concat(offerModelIds)
                        .Distinct()
                        .Count()
                }
            };
        }).OrderByDescending(x => x.Item.SiteScore).ToList();

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var pageRows = scored
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();
        var pageSiteIds = pageRows.Select(x => x.SiteId).ToList();
        var statusSince = DateTime.UtcNow.AddHours(-24);
        List<SiteStatusTestRow> statusTests = pageSiteIds.Count == 0
            ? []
            : await db.Queryable<TestRecordEntity>()
                .Where(x =>
                    pageSiteIds.Contains(x.SiteId) &&
                    x.TestedAt >= statusSince &&
                    x.TestType != "user" &&
                    x.TestType != "self")
                .Select(x => new SiteStatusTestRow
                {
                    SiteId = x.SiteId,
                    ModelId = x.ModelId,
                    Status = x.Status,
                    RiskScore = x.RiskScore,
                    RiskLevel = x.RiskLevel,
                    MatchScore = x.MatchScore,
                    TestedAt = x.TestedAt
                })
                .ToListAsync(cancellationToken);
        var status24hMap = BuildSiteStatus24hMap(statusTests, statusSince);
        List<SiteLatestTestRow> latestTestRows = pageSiteIds.Count == 0
            ? []
            : await db.Queryable<TestRecordEntity>()
                .Where(x => pageSiteIds.Contains(x.SiteId) && x.TestType != "user" && x.TestType != "self")
                .GroupBy(x => x.SiteId)
                .Select(x => new SiteLatestTestRow
                {
                    SiteId = x.SiteId,
                    TestedAt = SqlFunc.AggregateMax(x.TestedAt)
                })
                .ToListAsync(cancellationToken);
        var latestTestMap = latestTestRows.ToDictionary(x => x.SiteId, x => x.TestedAt);
        var items = pageRows
            .Select(row =>
            {
                var latestTestAt = latestTestMap.TryGetValue(row.SiteId, out var testedAt)
                    ? testedAt
                    : (DateTime?)null;
                return ApplySiteRuntimeStatus(
                    row.Item,
                    latestTestAt,
                    status24hMap.GetValueOrDefault(row.SiteId) ?? BuildEmptySiteStatus24h());
            })
            .ToList();

        return ToPaged(items, normalizedPage, normalizedPageSize, scored.Count);
    }

    public async Task<PagedResult<PublicModelCatalogItemResponse>> GetModelsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        RefAsync<int> total = 0;
        var models = await db.Queryable<AiModelEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "active")
            .OrderBy(x => x.SortOrder, OrderByType.Asc)
            .OrderBy(x => x.Id, OrderByType.Desc)
            .Select(x => new ModelCatalogRow
            {
                Id = x.Id,
                Slug = x.Slug,
                DisplayName = x.DisplayName,
                Vendor = x.Vendor,
                OfficialModelId = x.OfficialModelId,
                RequestName = x.RequestName,
                ApiType = x.ApiType
            })
            .ToPageListAsync(normalizedPage, normalizedPageSize, total, cancellationToken);

        var modelIds = models.Select(x => x.Id).ToList();
        List<ModelCoverageRow> coverageRows = modelIds.Count == 0
            ? []
            : await db.Queryable<RelayOfferEntity, RelaySiteEntity>(
                    (offer, site) => offer.SiteId == site.Id)
                .Where((offer, site) =>
                    modelIds.Contains(offer.ModelId) &&
                    offer.Status == "active" &&
                    site.Status == "active" &&
                    site.DeletedAt == null)
                .Select((offer, site) => new ModelCoverageRow
                {
                    ModelId = offer.ModelId,
                    SiteId = site.Id
                })
                .ToListAsync(cancellationToken);
        var coverageMap = coverageRows
            .GroupBy(x => x.ModelId)
            .ToDictionary(x => x.Key, x => x.Select(row => row.SiteId).Distinct().Count());

        var items = models.Select(model =>
        {
            return new PublicModelCatalogItemResponse
            {
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                Vendor = model.Vendor,
                OfficialModelId = model.OfficialModelId,
                RequestName = model.RequestName,
                ApiType = model.ApiType,
                RelaySiteCount = coverageMap.GetValueOrDefault(model.Id)
            };
        }).ToList();

        return ToPaged(items, normalizedPage, normalizedPageSize, total);
    }

    private async Task<Dictionary<string, RiskSnapshot>> LoadRiskMapAsync(IReadOnlyList<(string SiteSlug, string ModelSlug)> keys, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return [];
        }

        var siteSlugs = keys.Select(x => x.SiteSlug)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var modelSlugs = keys.Select(x => x.ModelSlug)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (siteSlugs.Count == 0 || modelSlugs.Count == 0)
        {
            return [];
        }

        var keySet = keys.Select(x => $"{x.SiteSlug}|{x.ModelSlug}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var risks = await db.Queryable<RiskEvidenceEntity, RelaySiteEntity, AiModelEntity>(
                (risk, site, model) => new JoinQueryInfos(
                    JoinType.Inner, risk.SiteId == site.Id,
                    JoinType.Inner, risk.ModelId == model.Id))
            .Where((risk, site, model) => siteSlugs.Contains(site.Slug) && modelSlugs.Contains(model.Slug))
            .Select((risk, site, model) => new
            {
                site.Slug,
                ModelSlug = model.Slug,
                risk.RiskScore
            })
            .ToListAsync(cancellationToken);

        return risks
            .Where(x => keySet.Contains($"{x.Slug}|{x.ModelSlug}"))
            .GroupBy(x => $"{x.Slug}|{x.ModelSlug}")
            .ToDictionary(x => x.Key, x =>
            {
                var score = x.Max(row => row.RiskScore);
                return new RiskSnapshot(score, ResolveRiskLevel(score));
            });
    }

    private static decimal Average(IEnumerable<decimal?> values)
    {
        var numeric = values.Where(x => x.HasValue).Select(x => x!.Value).ToList();
        return numeric.Count == 0 ? 0 : Math.Round(numeric.Average(), 2);
    }

    private static string ResolveRiskLevel(decimal? riskScore)
    {
        var score = riskScore ?? 0m;
        if (score >= 81) return "critical";
        if (score >= 51) return "high";
        if (score >= 21) return "medium";
        return "low";
    }

    private static PublicTestRecordListItemResponse ApplyRisk(PublicTestRecordListItemResponse item, RiskSnapshot? risk)
    {
        return new PublicTestRecordListItemResponse
        {
            Id = item.Id,
            PublicId = item.PublicId,
            SiteSlug = item.SiteSlug,
            SiteName = item.SiteName,
            SiteUrl = item.SiteUrl,
            ModelSlug = item.ModelSlug,
            ModelName = item.ModelName,
            TestType = item.TestType,
            Status = item.Status,
            FirstTokenMs = item.FirstTokenMs,
            FullResponseMs = item.FullResponseMs,
            ErrorMessage = item.ErrorMessage,
            RiskScore = risk?.Score ?? 0,
            RiskLevel = risk?.Level ?? "low",
            MatchScore = item.MatchScore,
            TestedAt = item.TestedAt
        };
    }

    private static PublicTestRecordListItemResponse MapListItem(TestRecordQueryRow row)
    {
        var siteName = FirstNonEmpty(row.SiteName, row.FallbackSiteName, row.FallbackSiteUrl, "未知站点");
        var modelName = FirstNonEmpty(row.ModelName, row.FallbackModelName, row.FallbackModelSlug, "未知模型");

        return new PublicTestRecordListItemResponse
        {
            Id = row.Id,
            PublicId = row.Id.ToString(),
            SiteSlug = row.SiteSlug ?? string.Empty,
            SiteName = siteName,
            SiteUrl = FirstNonEmpty(row.SiteUrl, row.FallbackSiteUrl, null),
            ModelSlug = FirstNonEmpty(row.ModelSlug, row.FallbackModelSlug, SlugHelper.Normalize(null, modelName)),
            ModelName = modelName,
            TestType = row.TestType,
            Status = row.Status,
            FirstTokenMs = row.FirstTokenMs,
            FullResponseMs = row.FullResponseMs,
            ErrorMessage = row.ErrorMessage,
            RiskScore = row.RiskScore,
            RiskLevel = string.IsNullOrWhiteSpace(row.RiskLevel) ? ResolveRiskLevel(row.RiskScore) : row.RiskLevel,
            MatchScore = row.MatchScore,
            TestedAt = row.TestedAt
        };
    }

    private static PublicTestRecordDetailResponse MapDetail(TestRecordQueryRow row)
    {
        var listItem = MapListItem(row);
        return new PublicTestRecordDetailResponse
        {
            Id = listItem.Id,
            PublicId = listItem.PublicId,
            SiteSlug = listItem.SiteSlug,
            SiteName = listItem.SiteName,
            SiteUrl = listItem.SiteUrl,
            ModelSlug = listItem.ModelSlug,
            ModelName = listItem.ModelName,
            TestType = listItem.TestType,
            Status = listItem.Status,
            FirstTokenMs = listItem.FirstTokenMs,
            FullResponseMs = listItem.FullResponseMs,
            ErrorMessage = listItem.ErrorMessage,
            RiskScore = listItem.RiskScore,
            RiskLevel = listItem.RiskLevel,
            TestedAt = listItem.TestedAt,
            ResultSummary = row.ResultSummary ?? string.Empty,
            MatchScore = row.MatchScore,
            InputTokens = row.InputTokens,
            OutputTokens = row.OutputTokens,
            TotalTokens = row.TotalTokens,
            EstimatedTokens = row.EstimatedTokens,
            TokensPerSecond = row.TokensPerSecond,
            IsStream = row.IsStream,
            Checks = DeserializeChecks(row.ChecksJson)
        };
    }

    private static PublicTestRecordListItemResponse MapSelfTestListItem(SelfTestEntity entity)
    {
        var siteName = HostFromUrl(entity.SiteUrl);
        var modelName = FirstNonEmpty(entity.ModelName, "未知模型");

        return new PublicTestRecordListItemResponse
        {
            Id = 0,
            PublicId = entity.Id,
            SiteSlug = string.Empty,
            SiteName = siteName,
            SiteUrl = entity.SiteUrl,
            ModelSlug = SlugHelper.Normalize(null, modelName),
            ModelName = modelName,
            TestType = "user",
            Status = NormalizeStatus(entity.Status),
            FirstTokenMs = entity.FirstTokenMs,
            FullResponseMs = entity.FullResponseMs,
            RiskScore = entity.RiskScore,
            RiskLevel = entity.RiskLevel,
            MatchScore = entity.MatchScore,
            TestedAt = entity.CreatedAt
        };
    }

    private static PublicTestRecordDetailResponse MapSelfTestDetail(SelfTestEntity entity)
    {
        var listItem = MapSelfTestListItem(entity);
        return new PublicTestRecordDetailResponse
        {
            Id = listItem.Id,
            PublicId = listItem.PublicId,
            SiteSlug = listItem.SiteSlug,
            SiteName = listItem.SiteName,
            SiteUrl = listItem.SiteUrl,
            ModelSlug = listItem.ModelSlug,
            ModelName = listItem.ModelName,
            TestType = listItem.TestType,
            Status = listItem.Status,
            FirstTokenMs = listItem.FirstTokenMs,
            FullResponseMs = listItem.FullResponseMs,
            ErrorMessage = null,
            RiskScore = listItem.RiskScore,
            RiskLevel = listItem.RiskLevel,
            TestedAt = listItem.TestedAt,
            ResultSummary = entity.ResultSummary,
            MatchScore = entity.MatchScore,
            InputTokens = entity.InputTokens,
            OutputTokens = entity.OutputTokens,
            TotalTokens = entity.TotalTokens,
            EstimatedTokens = entity.EstimatedTokens,
            TokensPerSecond = entity.TokensPerSecond,
            IsStream = entity.IsStream,
            Checks = DeserializeChecks(entity.ChecksJson)
        };
    }

    private static IReadOnlyList<PublicTestProbeResultResponse> DeserializeChecks(string? checksJson)
    {
        if (string.IsNullOrWhiteSpace(checksJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<PublicTestProbeResultResponse>>(checksJson, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static string HostFromUrl(string siteUrl)
    {
        return Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri) ? uri.Host : siteUrl;
    }

    private static string NormalizeStatus(string status)
    {
        return status.Equals("succeeded", StringComparison.OrdinalIgnoreCase) ? "success" : status;
    }

    private static PagedResult<T> ToPaged<T>(IReadOnlyList<T> items, int page, int pageSize, long total)
    {
        return new PagedResult<T>
        {
            Items = items,
            Page = Math.Max(page, 1),
            PageSize = pageSize,
            Total = total
        };
    }

    private static PublicRelaySiteRankingItemResponse ApplySiteRuntimeStatus(
        PublicRelaySiteRankingItemResponse item,
        DateTime? latestTestAt,
        PublicSiteStatus24hResponse status24h)
    {
        return new PublicRelaySiteRankingItemResponse
        {
            SiteSlug = item.SiteSlug,
            SiteName = item.SiteName,
            Description = item.Description,
            SupportsInvoice = item.SupportsInvoice,
            SupportsRefund = item.SupportsRefund,
            HasDocs = item.HasDocs,
            SiteScore = item.SiteScore,
            AvailabilityScore = item.AvailabilityScore,
            StabilityScore = item.StabilityScore,
            RiskScore = item.RiskScore,
            RiskLevel = item.RiskLevel,
            CoveredModelCount = item.CoveredModelCount,
            LatestTestAt = latestTestAt,
            Status24h = status24h
        };
    }

    private static Dictionary<ulong, PublicSiteStatus24hResponse> BuildSiteStatus24hMap(IReadOnlyList<SiteStatusTestRow> tests, DateTime since)
    {
        return tests
            .GroupBy(test => test.SiteId)
            .ToDictionary(group => group.Key, group => BuildSiteStatus24h(group.ToList(), since));
    }

    private static PublicSiteStatus24hResponse BuildSiteStatus24h(IReadOnlyList<SiteStatusTestRow> siteTests, DateTime since)
    {
        var totalTests = siteTests.Count;
        var successCount = siteTests.Count(test => IsSuccessStatus(test.Status));
        var scores = siteTests.Select(ResolveTestScore).ToList();
        var buckets = Enumerable.Range(0, 24)
            .Select(index => BuildSiteStatusBucket(
                siteTests.Where(test =>
                    test.TestedAt >= since.AddHours(index) &&
                    test.TestedAt < since.AddHours(index + 1))
                .ToList(),
                since.AddHours(index)))
            .ToList();

        return new PublicSiteStatus24hResponse
        {
            SuccessRate = totalTests == 0 ? 0 : Math.Round(successCount * 100m / totalTests, 1),
            TotalTests = totalTests,
            TestedModelCount = siteTests.Where(test => test.ModelId > 0).Select(test => test.ModelId).Distinct().Count(),
            AverageScore = scores.Count == 0 ? null : Math.Round(scores.Average(), 1),
            HealthyCount = buckets.Count(bucket => bucket.StatusTone == "success"),
            WarningCount = buckets.Count(bucket => bucket.StatusTone == "warning"),
            CriticalCount = buckets.Count(bucket => bucket.StatusTone == "danger"),
            LastTestedAt = siteTests.MaxBy(test => test.TestedAt)?.TestedAt,
            Buckets = buckets
        };
    }

    private static PublicSiteStatus24hResponse BuildEmptySiteStatus24h()
    {
        var since = DateTime.UtcNow.AddHours(-24);
        return new PublicSiteStatus24hResponse
        {
            Buckets = Enumerable.Range(0, 24)
                .Select(index => BuildSiteStatusBucket([], since.AddHours(index)))
                .ToList()
        };
    }

    private static PublicSiteStatusBucketResponse BuildSiteStatusBucket(IReadOnlyList<SiteStatusTestRow> bucketTests, DateTime slotStartAt)
    {
        var slotLabel = $"{slotStartAt.AddHours(8):HH}:00";
        if (bucketTests.Count == 0)
        {
            return new PublicSiteStatusBucketResponse
            {
                SlotLabel = slotLabel,
                SlotStartAt = slotStartAt,
                StatusTone = "neutral",
                StatusLabel = "暂无测试",
                HasTest = false,
                TotalTests = 0,
                SuccessCount = 0,
                SuccessRate = 0,
                TestedModelCount = 0
            };
        }

        var totalTests = bucketTests.Count;
        var successCount = bucketTests.Count(test => IsSuccessStatus(test.Status));
        var averageScore = Math.Round(bucketTests.Select(ResolveTestScore).Average(), 1);
        var testedModelCount = bucketTests.Where(test => test.ModelId > 0).Select(test => test.ModelId).Distinct().Count();
        var latestTestedAt = bucketTests.Max(test => test.TestedAt);
        var tone = ResolveScoreTone(averageScore);

        return new PublicSiteStatusBucketResponse
        {
            SlotLabel = slotLabel,
            SlotStartAt = slotStartAt,
            StatusTone = tone,
            StatusLabel = ResolveScoreLabel(averageScore),
            HasTest = true,
            TotalTests = totalTests,
            SuccessCount = successCount,
            SuccessRate = Math.Round(successCount * 100m / totalTests, 1),
            TestedModelCount = testedModelCount,
            AverageScore = averageScore,
            TestedAt = latestTestedAt
        };
    }

    private static decimal ResolveTestScore(SiteStatusTestRow test)
    {
        if (!IsSuccessStatus(test.Status))
        {
            return 0;
        }

        var score = test.MatchScore > 0 ? test.MatchScore : 100m - test.RiskScore;
        return Math.Clamp(score, 0m, 100m);
    }

    private static string ResolveScoreTone(decimal score)
    {
        if (score >= 80m) return "success";
        if (score >= 60m) return "warning";
        return "danger";
    }

    private static string ResolveScoreLabel(decimal score)
    {
        if (score >= 80m) return "稳定";
        if (score >= 60m) return "波动";
        return "偏低";
    }

    private static bool IsSuccessStatus(string status)
    {
        return string.Equals(status, "success", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record RiskSnapshot(decimal Score, string Level);

    private sealed class SiteModelCoverageRow
    {
        public ulong SiteId { get; init; }

        public ulong ModelId { get; init; }
    }

    private sealed class SiteRankingRow
    {
        public ulong SiteId { get; init; }

        public PublicRelaySiteRankingItemResponse Item { get; init; } = new();
    }

    private sealed class SiteLatestTestRow
    {
        public ulong SiteId { get; init; }

        public DateTime TestedAt { get; init; }
    }

    private sealed class SiteStatusTestRow
    {
        public ulong SiteId { get; init; }

        public ulong ModelId { get; init; }

        public string Status { get; init; } = string.Empty;

        public decimal RiskScore { get; init; }

        public string RiskLevel { get; init; } = "low";

        public decimal MatchScore { get; init; }

        public DateTime TestedAt { get; init; }
    }

    private sealed class TestRecordQueryRow
    {
        public ulong Id { get; init; }

        public string? SiteSlug { get; init; }

        public string? SiteName { get; init; }

        public string? SiteUrl { get; init; }

        public string? FallbackSiteName { get; init; }

        public string? FallbackSiteUrl { get; init; }

        public string? ModelSlug { get; init; }

        public string? ModelName { get; init; }

        public string? FallbackModelSlug { get; init; }

        public string? FallbackModelName { get; init; }

        public string TestType { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public int? FirstTokenMs { get; init; }

        public int? FullResponseMs { get; init; }

        public string? ErrorMessage { get; init; }

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

        public string? ChecksJson { get; init; }

        public DateTime TestedAt { get; init; }
    }

    private sealed class ModelCatalogRow
    {
        public ulong Id { get; init; }
        public string Slug { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Vendor { get; init; } = string.Empty;
        public string OfficialModelId { get; init; } = string.Empty;
        public string RequestName { get; init; } = string.Empty;
        public string ApiType { get; init; } = "openai";
    }

    private sealed class ModelCoverageRow
    {
        public ulong ModelId { get; init; }
        public ulong SiteId { get; init; }
    }

}
