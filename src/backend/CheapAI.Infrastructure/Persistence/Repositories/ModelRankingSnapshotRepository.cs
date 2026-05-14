using CheapAI.Application.Common.Paging;
using CheapAI.Application.Models;
using CheapAI.Application.Public;
using CheapAI.Application.RelaySites;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class ModelRankingSnapshotRepository(ISqlSugarClient db, IRelaySiteRepository relaySiteRepository, IModelRepository modelRepository)
    : IModelRankingSnapshotRepository
{
    public async Task<HomeOverviewResponse> GetHomeOverviewAsync(CancellationToken cancellationToken = default)
    {
        var latestSnapshotAt = await db.Queryable<ModelRankingSnapshotEntity>()
            .OrderByDescending(x => x.SnapshotAt)
            .Select(x => x.SnapshotAt)
            .FirstAsync(cancellationToken);

        var topPriceCards = await QueryCardsAsync("price", "7d", 5, cancellationToken);
        var topStabilityCards = await QueryCardsAsync("stability", "7d", 5, cancellationToken);
        var riskHighlights = await QueryRiskHighlightsAsync(5, cancellationToken);
        var popularModels = await QueryPopularModelsAsync(5, cancellationToken);

        return new HomeOverviewResponse
        {
            FeaturedModel = popularModels.FirstOrDefault()?.ModelSlug ?? topPriceCards.FirstOrDefault()?.ModelSlug ?? topStabilityCards.FirstOrDefault()?.ModelSlug,
            PopularModels = popularModels,
            Stats = new HomeOverviewStatsResponse
            {
                SiteCount = await relaySiteRepository.CountActiveAsync(cancellationToken),
                ModelCount = await modelRepository.CountActiveAsync(cancellationToken),
                TestCount = await db.Queryable<TestRecordEntity>().CountAsync(cancellationToken),
                LatestTestAt = latestSnapshotAt == default ? null : latestSnapshotAt
            },
            TopPriceCards = topPriceCards,
            TopStabilityCards = topStabilityCards,
            RiskHighlights = riskHighlights
        };
    }

    public async Task<ModelRankingResponse?> GetModelRankingsAsync(string modelSlug, ModelRankingQuery query, CancellationToken cancellationToken = default)
    {
        var model = await modelRepository.GetBySlugAsync(modelSlug, cancellationToken);
        if (model is null)
        {
            return null;
        }

        var rankingType = string.IsNullOrWhiteSpace(query.RankingType) ? "price" : query.RankingType;
        var window = string.IsNullOrWhiteSpace(query.Window) ? "7d" : query.Window;

        var sqlQuery = db.Queryable<ModelRankingSnapshotEntity, RelaySiteEntity>(
                (snapshot, site) => snapshot.SiteId == site.Id)
            .Where((snapshot, site) =>
                snapshot.ModelId == model.Id &&
                snapshot.RankingType == rankingType &&
                snapshot.WindowType == window &&
                site.DeletedAt == null);

        if (query.SupportsInvoice.HasValue)
        {
            sqlQuery = sqlQuery.Where((snapshot, site) => site.SupportsInvoice == query.SupportsInvoice.Value);
        }

        if (query.SupportsRefund.HasValue)
        {
            sqlQuery = sqlQuery.Where((snapshot, site) => site.SupportsRefund == query.SupportsRefund.Value);
        }

        if (query.HasDocs.HasValue)
        {
            sqlQuery = sqlQuery.Where((snapshot, site) => site.HasDocs == query.HasDocs.Value);
        }

        if (query.RiskFilter == "exclude-high")
        {
            sqlQuery = sqlQuery.Where((snapshot, site) => snapshot.RiskScore == null || snapshot.RiskScore < 51);
        }

        if (query.RiskFilter == "only-low-risk")
        {
            sqlQuery = sqlQuery.Where((snapshot, site) => snapshot.RiskScore != null && snapshot.RiskScore <= 20);
        }

        RefAsync<int> total = 0;
        var rawItems = await sqlQuery
            .OrderBy((snapshot, site) => snapshot.RankPosition, OrderByType.Asc)
            .Select((snapshot, site) => new ModelRankingRow
            {
                SiteSlug = site.Slug,
                SiteName = site.Name,
                EffectiveInputPriceUsd = snapshot.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = snapshot.EffectiveOutputPriceUsd,
                Availability24h = snapshot.AvailabilityScore,
                Stability7d = snapshot.StabilityScore,
                SpeedScore = snapshot.SpeedScore,
                RiskScore = snapshot.RiskScore,
                SupportsInvoice = site.SupportsInvoice,
                SupportsRefund = site.SupportsRefund,
                HasDocs = site.HasDocs
            })
            .ToPageListAsync(query.Page, query.PageSize, total, cancellationToken);

        var items = rawItems.Select(item => new ModelRankingItemResponse
        {
            SiteSlug = item.SiteSlug,
            SiteName = item.SiteName,
            EffectiveInputPriceUsd = item.EffectiveInputPriceUsd,
            EffectiveOutputPriceUsd = item.EffectiveOutputPriceUsd,
            Availability24h = item.Availability24h,
            Stability7d = item.Stability7d,
            FirstTokenMs = item.SpeedScore.HasValue ? (int?)Math.Round(item.SpeedScore.Value) : null,
            FullResponseMs = item.SpeedScore.HasValue ? (int?)Math.Round(item.SpeedScore.Value) : null,
            RiskScore = item.RiskScore,
            RiskLevel = ResolveRiskLevel(item.RiskScore),
            SupportsInvoice = item.SupportsInvoice,
            SupportsRefund = item.SupportsRefund,
            HasDocs = item.HasDocs
        }).ToList();

        var snapshotAt = await db.Queryable<ModelRankingSnapshotEntity>()
            .Where(x => x.ModelId == model.Id && x.RankingType == rankingType && x.WindowType == window)
            .OrderByDescending(x => x.SnapshotAt)
            .Select(x => x.SnapshotAt)
            .FirstAsync(cancellationToken);

        return new ModelRankingResponse
        {
            Model = new RankingModelSummary
            {
                Slug = model.Slug,
                DisplayName = model.DisplayName
            },
            RankingType = rankingType,
            Window = window,
            SnapshotAt = snapshotAt == default ? null : snapshotAt,
            Result = new PagedResult<ModelRankingItemResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                Total = total
            }
        };
    }

    private async Task<IReadOnlyList<RankingCardResponse>> QueryCardsAsync(string rankingType, string window, int limit, CancellationToken cancellationToken)
    {
        var items = await db.Queryable<ModelRankingSnapshotEntity, RelaySiteEntity, AiModelEntity>(
                (snapshot, site, model) => new JoinQueryInfos(
                    JoinType.Inner, snapshot.SiteId == site.Id,
                    JoinType.Inner, snapshot.ModelId == model.Id))
            .Where((snapshot, site, model) =>
                snapshot.RankingType == rankingType &&
                snapshot.WindowType == window &&
                snapshot.RankPosition == 1 &&
                site.DeletedAt == null &&
                model.DeletedAt == null)
            .OrderBy((snapshot, site, model) => snapshot.SnapshotAt, OrderByType.Desc)
            .Select((snapshot, site, model) => new RankingCardResponse
            {
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                SiteSlug = site.Slug,
                SiteName = site.Name,
                EffectiveInputPriceUsd = snapshot.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = snapshot.EffectiveOutputPriceUsd,
                StabilityScore = snapshot.StabilityScore,
                RiskScore = snapshot.RiskScore
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        return items;
    }

    private async Task<IReadOnlyList<HomePopularModelResponse>> QueryPopularModelsAsync(int limit, CancellationToken cancellationToken)
    {
        var items = await db.Queryable<AiModelEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "active" && x.IsHot)
            .OrderBy(x => x.SortOrder, OrderByType.Asc)
            .OrderBy(x => x.Id, OrderByType.Desc)
            .Select(x => new HomePopularModelResponse
            {
                ModelSlug = x.Slug,
                ModelName = x.DisplayName
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            return items;
        }

        return await db.Queryable<AiModelEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "active")
            .OrderBy(x => x.SortOrder, OrderByType.Asc)
            .OrderBy(x => x.Id, OrderByType.Desc)
            .Select(x => new HomePopularModelResponse
            {
                ModelSlug = x.Slug,
                ModelName = x.DisplayName
            })
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<RiskHighlightResponse>> QueryRiskHighlightsAsync(int limit, CancellationToken cancellationToken)
    {
        var items = await db.Queryable<ModelRankingSnapshotEntity, RelaySiteEntity, AiModelEntity>(
                (snapshot, site, model) => new JoinQueryInfos(
                    JoinType.Inner, snapshot.SiteId == site.Id,
                    JoinType.Inner, snapshot.ModelId == model.Id))
            .Where((snapshot, site, model) =>
                snapshot.RiskScore != null &&
                snapshot.RiskScore >= 51 &&
                site.DeletedAt == null &&
                model.DeletedAt == null)
            .OrderBy((snapshot, site, model) => snapshot.RiskScore, OrderByType.Desc)
            .Select((snapshot, site, model) => new RiskHighlightResponse
            {
                SiteSlug = site.Slug,
                SiteName = site.Name,
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                RiskScore = snapshot.RiskScore ?? 0
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        return items;
    }

    private static string ResolveRiskLevel(decimal? riskScore)
    {
        var score = riskScore ?? 0;
        if (score >= 81) return "critical";
        if (score >= 51) return "high";
        if (score >= 21) return "medium";
        return "low";
    }

    private sealed class ModelRankingRow
    {
        public string SiteSlug { get; init; } = string.Empty;

        public string SiteName { get; init; } = string.Empty;

        public decimal? EffectiveInputPriceUsd { get; init; }

        public decimal? EffectiveOutputPriceUsd { get; init; }

        public decimal? Availability24h { get; init; }

        public decimal? Stability7d { get; init; }

        public decimal? SpeedScore { get; init; }

        public decimal? RiskScore { get; init; }

        public bool SupportsInvoice { get; init; }

        public bool SupportsRefund { get; init; }

        public bool HasDocs { get; init; }
    }
}
