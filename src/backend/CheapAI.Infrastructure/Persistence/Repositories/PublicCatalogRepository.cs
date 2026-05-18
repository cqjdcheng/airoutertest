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
            .ToListAsync(cancellationToken);

        var items = rows.Select(MapListItem).ToList();
        var linkedSelfTestIds = await db.Queryable<TestRecordEntity>()
            .Where(x => x.SelfTestId != null)
            .Select(x => x.SelfTestId)
            .ToListAsync(cancellationToken);

        var linkedSelfTestIdSet = linkedSelfTestIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selfTests = await db.Queryable<SelfTestEntity>()
            .Where(x => x.ExpiresAt > DateTime.UtcNow)
            .OrderBy(x => x.CreatedAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        items.AddRange(selfTests
            .Where(x => !linkedSelfTestIdSet.Contains(x.Id))
            .Where(x => string.IsNullOrWhiteSpace(testType) || IsSelfTestType(testType))
            .Select(MapSelfTestListItem));

        var riskMap = await LoadRiskMapAsync(items.Select(x => (x.SiteSlug, x.ModelSlug)).Distinct().ToList(), cancellationToken);
        items = items.Select(row =>
        {
            var risk = riskMap.GetValueOrDefault($"{row.SiteSlug}|{row.ModelSlug}");
            return row.RiskScore > 0 ? row : ApplyRisk(row, risk);
        })
            .OrderByDescending(x => x.TestedAt)
            .ToList();

        return ToPaged(items.Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, items.Count);
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

        var snapshots = await db.Queryable<ModelRankingSnapshotEntity>()
            .Where(x => x.RankingType == "value" && x.WindowType == "7d")
            .ToListAsync(cancellationToken);

        var offers = await db.Queryable<RelayOfferEntity>()
            .Where(x => x.Status == "active")
            .ToListAsync(cancellationToken);

        var tests = await db.Queryable<TestRecordEntity>()
            .OrderBy(x => x.TestedAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        var scored = sites.Select(site =>
        {
            var siteSnapshots = snapshots.Where(x => x.SiteId == site.Id).ToList();
            var siteTests = tests.Where(x => x.SiteId == site.Id).ToList();
            var availability = Average(siteSnapshots.Select(x => x.AvailabilityScore));
            var stability = Average(siteSnapshots.Select(x => x.StabilityScore));
            var risk = siteSnapshots.Count == 0 ? 0m : siteSnapshots.Max(x => x.RiskScore ?? 0m);
            var enterpriseScore = (site.SupportsInvoice ? 34m : 0m) + (site.SupportsRefund ? 33m : 0m) + (site.HasDocs ? 33m : 0m);
            var score = Math.Round(availability * 0.35m + stability * 0.25m + Math.Max(0, 100 - risk) * 0.25m + enterpriseScore * 0.15m, 2);

            return new PublicRelaySiteRankingItemResponse
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
                    .Concat(offers.Where(x => x.SiteId == site.Id).Select(x => x.ModelId))
                    .Distinct()
                    .Count(),
                LatestTestAt = siteTests.FirstOrDefault()?.TestedAt
            };
        }).OrderByDescending(x => x.SiteScore).ToList();

        return ToPaged(scored.Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, scored.Count);
    }

    public async Task<PagedResult<PublicModelCatalogItemResponse>> GetModelsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var models = await db.Queryable<AiModelEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "active")
            .OrderBy(x => x.SortOrder, OrderByType.Asc)
            .OrderBy(x => x.Id, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        var snapshots = await db.Queryable<ModelRankingSnapshotEntity, RelaySiteEntity>(
                (snapshot, site) => snapshot.SiteId == site.Id)
            .Where((snapshot, site) =>
                snapshot.RankingType == "price" &&
                snapshot.WindowType == "7d" &&
                site.DeletedAt == null)
            .Select((snapshot, site) => new ModelSnapshotRow
            {
                ModelId = snapshot.ModelId,
                SiteSlug = site.Slug,
                SiteName = site.Name,
                EffectiveInputPriceUsd = snapshot.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = snapshot.EffectiveOutputPriceUsd,
                StabilityScore = snapshot.StabilityScore,
                RiskScore = snapshot.RiskScore,
                RankPosition = snapshot.RankPosition
            })
            .ToListAsync(cancellationToken);

        var offers = await QueryActiveOfferRowsAsync(cancellationToken);

        var items = models.Select(model =>
        {
            var modelSnapshots = snapshots.Where(x => x.ModelId == model.Id).OrderBy(x => x.RankPosition).ToList();
            var modelOffers = offers
                .Where(x => x.ModelId == model.Id)
                .OrderBy(x => x.PriceSort)
                .ToList();
            var best = modelSnapshots.FirstOrDefault();
            var fallback = modelOffers.FirstOrDefault();
            return new PublicModelCatalogItemResponse
            {
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                Vendor = model.Vendor,
                OfficialModelId = model.OfficialModelId,
                RequestName = model.RequestName,
                ApiType = model.ApiType,
                OfficialInputPriceUsd = model.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = model.OfficialOutputPriceUsd,
                CheapestSiteSlug = best?.SiteSlug ?? fallback?.SiteSlug,
                CheapestSiteName = best?.SiteName ?? fallback?.SiteName,
                EffectiveInputPriceUsd = best?.EffectiveInputPriceUsd ?? fallback?.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = best?.EffectiveOutputPriceUsd ?? fallback?.EffectiveOutputPriceUsd,
                StabilityScore = best?.StabilityScore,
                RiskScore = best?.RiskScore,
                RiskLevel = ResolveRiskLevel(best?.RiskScore),
                RelaySiteCount = modelSnapshots.Select(x => x.SiteSlug)
                    .Concat(modelOffers.Select(x => x.SiteSlug))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count()
            };
        }).ToList();

        return ToPaged(items.Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, items.Count);
    }

    public async Task<IReadOnlyList<PublicCheapestRankingResponse>> GetCheapestRankingsAsync(IReadOnlyList<string> modelSlugs, int limit, CancellationToken cancellationToken = default)
    {
        var result = new List<PublicCheapestRankingResponse>();
        foreach (var modelSlug in modelSlugs.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var model = await db.Queryable<AiModelEntity>()
                .FirstAsync(x => x.Slug == modelSlug && x.DeletedAt == null, cancellationToken);
            if (model is null)
            {
                continue;
            }

            var rows = await db.Queryable<ModelRankingSnapshotEntity, RelaySiteEntity>(
                    (snapshot, site) => snapshot.SiteId == site.Id)
                .Where((snapshot, site) =>
                    snapshot.ModelId == model.Id &&
                    snapshot.RankingType == "price" &&
                    snapshot.WindowType == "7d" &&
                    site.DeletedAt == null)
                .OrderBy((snapshot, site) => snapshot.RankPosition, OrderByType.Asc)
                .Select((snapshot, site) => new ModelRankingItemResponse
                {
                    SiteSlug = site.Slug,
                    SiteName = site.Name,
                    EffectiveInputPriceUsd = snapshot.EffectiveInputPriceUsd,
                    EffectiveOutputPriceUsd = snapshot.EffectiveOutputPriceUsd,
                    Availability24h = snapshot.AvailabilityScore,
                    Stability7d = snapshot.StabilityScore,
                    FirstTokenMs = null,
                    FullResponseMs = null,
                    RiskScore = snapshot.RiskScore,
                    RiskLevel = "low",
                    SupportsInvoice = site.SupportsInvoice,
                    SupportsRefund = site.SupportsRefund,
                    HasDocs = site.HasDocs
                })
                .Take(limit)
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
            {
                rows = await QueryOfferRankingRowsAsync(model.Id, limit, cancellationToken);
            }

            result.Add(new PublicCheapestRankingResponse
            {
                ModelSlug = modelSlug,
                Items = rows.Select(ApplyRiskLevel).ToList()
            });
        }

        return result;
    }

    private async Task<Dictionary<string, RiskSnapshot>> LoadRiskMapAsync(IReadOnlyList<(string SiteSlug, string ModelSlug)> keys, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return [];
        }

        var risks = await db.Queryable<RiskEvidenceEntity, RelaySiteEntity, AiModelEntity>(
                (risk, site, model) => new JoinQueryInfos(
                    JoinType.Inner, risk.SiteId == site.Id,
                    JoinType.Inner, risk.ModelId == model.Id))
            .Select((risk, site, model) => new
            {
                site.Slug,
                ModelSlug = model.Slug,
                risk.RiskScore
            })
            .ToListAsync(cancellationToken);

        return risks
            .GroupBy(x => $"{x.Slug}|{x.ModelSlug}")
            .ToDictionary(x => x.Key, x =>
            {
                var score = x.Max(row => row.RiskScore);
                return new RiskSnapshot(score, ResolveRiskLevel(score));
            });
    }

    private async Task<IReadOnlyList<ModelOfferRow>> QueryActiveOfferRowsAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Queryable<RelayOfferEntity, RelaySiteEntity>(
                (offer, site) => offer.SiteId == site.Id)
            .Where((offer, site) =>
                offer.Status == "active" &&
                site.Status == "active" &&
                site.DeletedAt == null)
            .Select((offer, site) => new ModelOfferRow
            {
                ModelId = offer.ModelId,
                SiteSlug = site.Slug,
                SiteName = site.Name,
                EffectiveInputPriceUsd = offer.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = offer.EffectiveOutputPriceUsd
            })
            .ToListAsync(cancellationToken);

        return rows.Select(x => x with
        {
            PriceSort = (x.EffectiveInputPriceUsd ?? 999999m) + (x.EffectiveOutputPriceUsd ?? 999999m)
        }).ToList();
    }

    private async Task<List<ModelRankingItemResponse>> QueryOfferRankingRowsAsync(ulong modelId, int limit, CancellationToken cancellationToken)
    {
        var rows = await db.Queryable<RelayOfferEntity, RelaySiteEntity>(
                (offer, site) => offer.SiteId == site.Id)
            .Where((offer, site) =>
                offer.ModelId == modelId &&
                offer.Status == "active" &&
                site.Status == "active" &&
                site.DeletedAt == null)
            .Select((offer, site) => new ModelOfferRankingRow
            {
                SiteId = site.Id,
                SiteSlug = site.Slug,
                SiteName = site.Name,
                EffectiveInputPriceUsd = offer.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = offer.EffectiveOutputPriceUsd,
                SupportsInvoice = site.SupportsInvoice,
                SupportsRefund = site.SupportsRefund,
                HasDocs = site.HasDocs
            })
            .ToListAsync(cancellationToken);

        var riskRows = await db.Queryable<RiskEvidenceEntity>()
            .Where(x => x.ModelId == modelId)
            .Select(x => new
            {
                x.SiteId,
                x.RiskScore
            })
            .ToListAsync(cancellationToken);

        var riskMap = riskRows
            .GroupBy(x => x.SiteId)
            .ToDictionary(x => x.Key, x => (decimal?)x.Max(row => row.RiskScore));

        return rows
            .OrderBy(x => (x.EffectiveInputPriceUsd ?? 999999m) + (x.EffectiveOutputPriceUsd ?? 999999m))
            .Take(limit)
            .Select(x =>
            {
                var riskScore = riskMap.GetValueOrDefault(x.SiteId);
                return new ModelRankingItemResponse
                {
                    SiteSlug = x.SiteSlug,
                    SiteName = x.SiteName,
                    EffectiveInputPriceUsd = x.EffectiveInputPriceUsd,
                    EffectiveOutputPriceUsd = x.EffectiveOutputPriceUsd,
                    Availability24h = null,
                    Stability7d = null,
                    FirstTokenMs = null,
                    FullResponseMs = null,
                    RiskScore = riskScore,
                    RiskLevel = ResolveRiskLevel(riskScore),
                    SupportsInvoice = x.SupportsInvoice,
                    SupportsRefund = x.SupportsRefund,
                    HasDocs = x.HasDocs
                };
            })
            .ToList();
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

    private static bool IsSelfTestType(string? testType)
    {
        return string.IsNullOrWhiteSpace(testType) ||
               testType.Equals("user", StringComparison.OrdinalIgnoreCase) ||
               testType.Equals("self", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeStatus(string status)
    {
        return status.Equals("succeeded", StringComparison.OrdinalIgnoreCase) ? "success" : status;
    }

    private static ModelRankingItemResponse ApplyRiskLevel(ModelRankingItemResponse item)
    {
        return new ModelRankingItemResponse
        {
            SiteSlug = item.SiteSlug,
            SiteName = item.SiteName,
            EffectiveInputPriceUsd = item.EffectiveInputPriceUsd,
            EffectiveOutputPriceUsd = item.EffectiveOutputPriceUsd,
            Availability24h = item.Availability24h,
            Stability7d = item.Stability7d,
            FirstTokenMs = item.FirstTokenMs,
            FullResponseMs = item.FullResponseMs,
            RiskScore = item.RiskScore,
            RiskLevel = ResolveRiskLevel(item.RiskScore),
            SupportsInvoice = item.SupportsInvoice,
            SupportsRefund = item.SupportsRefund,
            HasDocs = item.HasDocs
        };
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

    private sealed record RiskSnapshot(decimal Score, string Level);

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

    private sealed class ModelSnapshotRow
    {
        public ulong ModelId { get; init; }
        public string SiteSlug { get; init; } = string.Empty;
        public string SiteName { get; init; } = string.Empty;
        public decimal? EffectiveInputPriceUsd { get; init; }
        public decimal? EffectiveOutputPriceUsd { get; init; }
        public decimal? StabilityScore { get; init; }
        public decimal? RiskScore { get; init; }
        public int RankPosition { get; init; }
    }

    private sealed record ModelOfferRow
    {
        public ulong ModelId { get; init; }
        public string SiteSlug { get; init; } = string.Empty;
        public string SiteName { get; init; } = string.Empty;
        public decimal? EffectiveInputPriceUsd { get; init; }
        public decimal? EffectiveOutputPriceUsd { get; init; }
        public decimal PriceSort { get; init; }
    }

    private sealed class ModelOfferRankingRow
    {
        public ulong SiteId { get; init; }
        public string SiteSlug { get; init; } = string.Empty;
        public string SiteName { get; init; } = string.Empty;
        public decimal? EffectiveInputPriceUsd { get; init; }
        public decimal? EffectiveOutputPriceUsd { get; init; }
        public bool SupportsInvoice { get; init; }
        public bool SupportsRefund { get; init; }
        public bool HasDocs { get; init; }
    }
}
