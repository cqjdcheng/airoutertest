using CheapAI.Application.Public;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class PublicSiteQueryRepository(ISqlSugarClient db) : IPublicSiteQueryRepository
{
    public async Task<SiteDetailResponse?> GetSiteDetailAsync(string siteSlug, string? window, string? modelSlug, CancellationToken cancellationToken = default)
    {
        var site = await db.Queryable<RelaySiteEntity>()
            .FirstAsync(x => x.Slug == siteSlug && x.DeletedAt == null, cancellationToken);

        if (site is null)
        {
            return null;
        }

        var rankingWindow = string.IsNullOrWhiteSpace(window) ? "7d" : window;

        var query = db.Queryable<ModelRankingSnapshotEntity, AiModelEntity>(
                (snapshot, model) => snapshot.ModelId == model.Id)
            .Where((snapshot, model) =>
                snapshot.SiteId == site.Id &&
                snapshot.RankingType == "price" &&
                snapshot.WindowType == rankingWindow &&
                model.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(modelSlug))
        {
            query = query.Where((snapshot, model) => model.Slug == modelSlug);
        }

        var rows = await query
            .OrderBy((snapshot, model) => snapshot.RankPosition, OrderByType.Asc)
            .Select((snapshot, model) => new
            {
                model.Slug,
                model.DisplayName,
                snapshot.EffectiveInputPriceUsd,
                snapshot.EffectiveOutputPriceUsd,
                snapshot.AvailabilityScore,
                snapshot.StabilityScore,
                snapshot.RiskScore
            })
            .ToListAsync(cancellationToken);

        var dedupedRows = rows
            .GroupBy(x => x.Slug)
            .Select(x => x.First())
            .ToList();

        var maxRiskScore = dedupedRows.Count == 0 ? (decimal?)null : dedupedRows.Max(x => x.RiskScore ?? 0m);

        return new SiteDetailResponse
        {
            Site = new PublicSiteSummaryResponse
            {
                Slug = site.Slug,
                Name = site.Name,
                BaseUrl = site.BaseUrl,
                WebsiteUrl = site.WebsiteUrl,
                Description = site.Description,
                SupportsRefund = site.SupportsRefund,
                SupportsInvoice = site.SupportsInvoice,
                HasDocs = site.HasDocs,
                DocsUrl = site.DocsUrl,
                Status = site.Status,
                InviteUrl = site.InviteUrl,
                RecentReview = site.RecentReview
            },
            SupportedModels = dedupedRows.Select(x => new PublicSiteSupportedModelResponse
            {
                ModelSlug = x.Slug,
                ModelName = x.DisplayName
            }).ToList(),
            Pricing = dedupedRows.Select(x => new PublicSitePricingResponse
            {
                ModelSlug = x.Slug,
                ModelName = x.DisplayName,
                EffectiveInputPriceUsd = x.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = x.EffectiveOutputPriceUsd
            }).ToList(),
            LatestTests = dedupedRows.Select(x => new PublicSiteLatestTestResponse
            {
                ModelSlug = x.Slug,
                ModelName = x.DisplayName,
                Availability24h = x.AvailabilityScore,
                Stability7d = x.StabilityScore,
                RiskScore = x.RiskScore
            }).ToList(),
            RiskSummary = new PublicSiteRiskSummaryResponse
            {
                MaxRiskScore = maxRiskScore,
                RiskLevel = ResolveRiskLevel(maxRiskScore)
            },
            Trends = new PublicSiteTrendsResponse
            {
                Price =
                [
                    new PublicTrendPointResponse { Label = "当前", Value = dedupedRows.FirstOrDefault()?.EffectiveInputPriceUsd ?? 0m }
                ],
                Stability =
                [
                    new PublicTrendPointResponse { Label = rankingWindow, Value = dedupedRows.FirstOrDefault()?.StabilityScore ?? 0m }
                ]
            }
        };
    }

    private static string ResolveRiskLevel(decimal? score)
    {
        var value = score ?? 0m;
        if (value >= 81m) return "critical";
        if (value >= 51m) return "high";
        if (value >= 21m) return "medium";
        return "low";
    }
}
