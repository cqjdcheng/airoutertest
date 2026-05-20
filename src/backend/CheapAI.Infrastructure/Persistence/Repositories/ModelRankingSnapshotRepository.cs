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

        if (rankingType.Equals("price", StringComparison.OrdinalIgnoreCase))
        {
            var offerRanking = await QueryOfferRankingRowsAsync(model.Id, query, cancellationToken);
            return new ModelRankingResponse
            {
                Model = new RankingModelSummary
                {
                    Slug = model.Slug,
                    DisplayName = model.DisplayName
                },
                RankingType = rankingType,
                Window = window,
                SnapshotAt = offerRanking.SnapshotAt,
                Result = new PagedResult<ModelRankingItemResponse>
                {
                    Items = offerRanking.Items.Select(MapRankingItem).ToList(),
                    Page = query.Page,
                    PageSize = query.PageSize,
                    Total = offerRanking.Total
                }
            };
        }

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
                SiteId = site.Id,
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

        var items = rawItems.Select(MapRankingItem).ToList();

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
        if (rankingType.Equals("price", StringComparison.OrdinalIgnoreCase))
        {
            return await QueryPriceCardsFromOffersAsync(limit, cancellationToken);
        }

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

    private async Task<IReadOnlyList<RankingCardResponse>> QueryPriceCardsFromOffersAsync(int limit, CancellationToken cancellationToken)
    {
        var rows = await db.Queryable<RelayOfferEntity, RelaySiteEntity, AiModelEntity>(
                (offer, site, model) => new JoinQueryInfos(
                    JoinType.Inner, offer.SiteId == site.Id,
                    JoinType.Inner, offer.ModelId == model.Id))
            .Where((offer, site, model) =>
                offer.Status == "active" &&
                site.Status == "active" &&
                site.DeletedAt == null &&
                model.DeletedAt == null)
            .Select((offer, site, model) => new
            {
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                SiteSlug = site.Slug,
                SiteName = site.Name,
                offer.EffectiveInputPriceUsd,
                offer.EffectiveOutputPriceUsd,
                offer.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(row => (row.EffectiveInputPriceUsd ?? 999999m) + (row.EffectiveOutputPriceUsd ?? 999999m))
            .GroupBy(row => row.ModelSlug)
            .Select(group => group.First())
            .Take(limit)
            .Select(row => new RankingCardResponse
            {
                ModelSlug = row.ModelSlug,
                ModelName = row.ModelName,
                SiteSlug = row.SiteSlug,
                SiteName = row.SiteName,
                EffectiveInputPriceUsd = row.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = row.EffectiveOutputPriceUsd,
                StabilityScore = null,
                RiskScore = null
            })
            .ToList();
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
                ModelName = x.DisplayName,
                RequestName = x.RequestName,
                ApiType = x.ApiType
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
                ModelName = x.DisplayName,
                RequestName = x.RequestName,
                ApiType = x.ApiType
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

    private async Task<OfferRankingFallback> QueryOfferRankingRowsAsync(ulong modelId, ModelRankingQuery query, CancellationToken cancellationToken)
    {
        var offerRows = await db.Queryable<RelayOfferEntity, RelaySiteEntity>(
                (offer, site) => offer.SiteId == site.Id)
            .Where((offer, site) =>
                offer.ModelId == modelId &&
                offer.Status == "active" &&
                site.Status == "active" &&
                site.DeletedAt == null)
            .Select((offer, site) => new OfferRankingRow
            {
                SiteId = site.Id,
                SiteSlug = site.Slug,
                SiteName = site.Name,
                EffectiveInputPriceUsd = offer.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = offer.EffectiveOutputPriceUsd,
                SupportsInvoice = site.SupportsInvoice,
                SupportsRefund = site.SupportsRefund,
                HasDocs = site.HasDocs,
                UpdatedAt = offer.UpdatedAt
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

        var filtered = offerRows.Select(row =>
            {
                var riskScore = riskMap.GetValueOrDefault(row.SiteId);
                return row with { RiskScore = riskScore };
            })
            .Where(row => !query.SupportsInvoice.HasValue || row.SupportsInvoice == query.SupportsInvoice.Value)
            .Where(row => !query.SupportsRefund.HasValue || row.SupportsRefund == query.SupportsRefund.Value)
            .Where(row => !query.HasDocs.HasValue || row.HasDocs == query.HasDocs.Value)
            .Where(row => query.RiskFilter != "exclude-high" || row.RiskScore is null || row.RiskScore < 51)
            .Where(row => query.RiskFilter != "only-low-risk" || row.RiskScore is <= 20)
            .OrderBy(row => (row.EffectiveInputPriceUsd ?? 999999m) + (row.EffectiveOutputPriceUsd ?? 999999m))
            .ToList();

        var items = filtered
            .Skip((Math.Max(query.Page, 1) - 1) * Math.Clamp(query.PageSize, 1, 200))
            .Take(Math.Clamp(query.PageSize, 1, 200))
            .Select(row => new ModelRankingRow
            {
                SiteId = row.SiteId,
                SiteSlug = row.SiteSlug,
                SiteName = row.SiteName,
                EffectiveInputPriceUsd = row.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = row.EffectiveOutputPriceUsd,
                Availability24h = null,
                Stability7d = null,
                SpeedScore = null,
                RiskScore = row.RiskScore,
                SupportsInvoice = row.SupportsInvoice,
                SupportsRefund = row.SupportsRefund,
                HasDocs = row.HasDocs
            })
            .ToList();

        return new OfferRankingFallback(
            items,
            filtered.Count,
            filtered.Count == 0 ? null : filtered.Max(row => row.UpdatedAt));
    }

    private static string ResolveRiskLevel(decimal? riskScore)
    {
        var score = riskScore ?? 0;
        if (score >= 81) return "critical";
        if (score >= 51) return "high";
        if (score >= 21) return "medium";
        return "low";
    }

    private static ModelRankingItemResponse MapRankingItem(ModelRankingRow item)
    {
        return new ModelRankingItemResponse
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
        };
    }

    private sealed class ModelRankingRow
    {
        public ulong SiteId { get; init; }

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

    private sealed record OfferRankingFallback(
        List<ModelRankingRow> Items,
        int Total,
        DateTime? SnapshotAt);

    private sealed record OfferRankingRow
    {
        public ulong SiteId { get; init; }
        public string SiteSlug { get; init; } = string.Empty;
        public string SiteName { get; init; } = string.Empty;
        public decimal? EffectiveInputPriceUsd { get; init; }
        public decimal? EffectiveOutputPriceUsd { get; init; }
        public bool SupportsInvoice { get; init; }
        public bool SupportsRefund { get; init; }
        public bool HasDocs { get; init; }
        public DateTime UpdatedAt { get; init; }
        public decimal? RiskScore { get; init; }
    }
}
