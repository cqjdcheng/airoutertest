using CheapAI.Application.Common.Paging;
using CheapAI.Application.Public;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class PublicCatalogRepository(ISqlSugarClient db) : IPublicCatalogRepository
{
    public async Task<PagedResult<PublicTestRecordListItemResponse>> GetLatestTestsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        RefAsync<int> total = 0;
        var rows = await db.Queryable<TestRecordEntity, RelaySiteEntity, AiModelEntity>(
                (record, site, model) => new JoinQueryInfos(
                    JoinType.Inner, record.SiteId == site.Id,
                    JoinType.Inner, record.ModelId == model.Id))
            .Where((record, site, model) => site.DeletedAt == null && model.DeletedAt == null)
            .OrderBy((record, site, model) => record.TestedAt, OrderByType.Desc)
            .Select((record, site, model) => new PublicTestRecordListItemResponse
            {
                Id = record.Id,
                SiteSlug = site.Slug,
                SiteName = site.Name,
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                TestType = record.TestType,
                Status = record.Status,
                FirstTokenMs = record.FirstTokenMs,
                FullResponseMs = record.FullResponseMs,
                ErrorMessage = record.ErrorMessage,
                RiskScore = 0,
                RiskLevel = "low",
                TestedAt = record.TestedAt
            })
            .ToPageListAsync(page, pageSize, total, cancellationToken);

        var riskMap = await LoadRiskMapAsync(rows.Select(x => (x.SiteSlug, x.ModelSlug)).Distinct().ToList(), cancellationToken);
        var items = rows.Select(row =>
        {
            var risk = riskMap.GetValueOrDefault($"{row.SiteSlug}|{row.ModelSlug}");
            return ApplyRisk(row, risk);
        }).ToList();

        return ToPaged(items, page, pageSize, total);
    }

    public async Task<PagedResult<PublicRelaySiteRankingItemResponse>> GetRelaySitesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var sites = await db.Queryable<RelaySiteEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "active")
            .ToListAsync(cancellationToken);

        var snapshots = await db.Queryable<ModelRankingSnapshotEntity>()
            .Where(x => x.RankingType == "value" && x.WindowType == "7d")
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
                CoveredModelCount = siteSnapshots.Select(x => x.ModelId).Distinct().Count(),
                LatestTestAt = siteTests.FirstOrDefault()?.TestedAt
            };
        }).OrderByDescending(x => x.SiteScore).ToList();

        return ToPaged(scored.Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, scored.Count);
    }

    public async Task<PagedResult<PublicModelCatalogItemResponse>> GetModelsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var models = await db.Queryable<AiModelEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "active")
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

        var items = models.Select(model =>
        {
            var modelSnapshots = snapshots.Where(x => x.ModelId == model.Id).OrderBy(x => x.RankPosition).ToList();
            var best = modelSnapshots.FirstOrDefault();
            return new PublicModelCatalogItemResponse
            {
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                Vendor = model.Vendor,
                OfficialModelId = model.OfficialModelId,
                OfficialInputPriceUsd = model.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = model.OfficialOutputPriceUsd,
                CheapestSiteSlug = best?.SiteSlug,
                CheapestSiteName = best?.SiteName,
                EffectiveInputPriceUsd = best?.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = best?.EffectiveOutputPriceUsd,
                StabilityScore = best?.StabilityScore,
                RiskScore = best?.RiskScore,
                RiskLevel = ResolveRiskLevel(best?.RiskScore),
                RelaySiteCount = modelSnapshots.Select(x => x.SiteSlug).Distinct().Count()
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
            SiteSlug = item.SiteSlug,
            SiteName = item.SiteName,
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
}
