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

        var offerQuery = db.Queryable<RelayOfferEntity, AiModelEntity>(
                (offer, model) => offer.ModelId == model.Id)
            .Where((offer, model) =>
                offer.SiteId == site.Id &&
                offer.Status == "active" &&
                model.DeletedAt == null &&
                model.Status == "active");

        if (!string.IsNullOrWhiteSpace(modelSlug))
        {
            offerQuery = offerQuery.Where((offer, model) => model.Slug == modelSlug);
        }

        var offerRows = await offerQuery
            .OrderBy((offer, model) => offer.EffectiveInputPriceUsd, OrderByType.Asc)
            .Select((offer, model) => new SiteModelPriceRow
            {
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                EffectiveInputPriceUsd = offer.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = offer.EffectiveOutputPriceUsd
            })
            .ToListAsync(cancellationToken);

        var snapshotQuery = db.Queryable<ModelRankingSnapshotEntity, AiModelEntity>(
                (snapshot, model) => snapshot.ModelId == model.Id)
            .Where((snapshot, model) =>
                snapshot.SiteId == site.Id &&
                snapshot.RankingType == "price" &&
                snapshot.WindowType == rankingWindow &&
                model.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(modelSlug))
        {
            snapshotQuery = snapshotQuery.Where((snapshot, model) => model.Slug == modelSlug);
        }

        var snapshotRows = await snapshotQuery
            .OrderBy((snapshot, model) => snapshot.RankPosition, OrderByType.Asc)
            .Select((snapshot, model) => new SiteSnapshotRow
            {
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                EffectiveInputPriceUsd = snapshot.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = snapshot.EffectiveOutputPriceUsd,
                AvailabilityScore = snapshot.AvailabilityScore,
                StabilityScore = snapshot.StabilityScore,
                RiskScore = snapshot.RiskScore
            })
            .ToListAsync(cancellationToken);

        var modelPriceRows = offerRows.Count > 0
            ? offerRows
            : snapshotRows.Select(x => new SiteModelPriceRow
            {
                ModelSlug = x.ModelSlug,
                ModelName = x.ModelName,
                EffectiveInputPriceUsd = x.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = x.EffectiveOutputPriceUsd
            }).ToList();

        var dedupedRows = modelPriceRows
            .GroupBy(x => x.ModelSlug)
            .Select(x => x.First())
            .ToList();

        var recent24hTests = await QueryRecent24hTestsAsync(site.Id, modelSlug, cancellationToken);
        var latestTests = await QueryLatestTestsAsync(site.Id, modelSlug, cancellationToken);
        var maxRiskScore = latestTests.Count > 0
            ? latestTests.Max(x => x.RiskScore ?? 0m)
            : snapshotRows.Count == 0 ? (decimal?)null : snapshotRows.Max(x => x.RiskScore ?? 0m);

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
                ModelSlug = x.ModelSlug,
                ModelName = x.ModelName
            }).ToList(),
            Pricing = dedupedRows.Select(x => new PublicSitePricingResponse
            {
                ModelSlug = x.ModelSlug,
                ModelName = x.ModelName,
                EffectiveInputPriceUsd = x.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = x.EffectiveOutputPriceUsd
            }).ToList(),
            LatestTests = latestTests,
            Status24h = BuildSiteStatus24h(recent24hTests),
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
                    new PublicTrendPointResponse { Label = rankingWindow, Value = snapshotRows.FirstOrDefault()?.StabilityScore ?? 0m }
                ]
            }
        };
    }

    private async Task<List<SiteTestRecordRow>> QueryRecent24hTestsAsync(ulong siteId, string? modelSlug, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var query = db.Queryable<TestRecordEntity, AiModelEntity>(
                (record, model) => record.ModelId == model.Id)
            .Where((record, model) =>
                record.SiteId == siteId &&
                record.TestedAt >= since &&
                model.DeletedAt == null &&
                model.Status == "active" &&
                record.TestType != "user" &&
                record.TestType != "self");

        if (!string.IsNullOrWhiteSpace(modelSlug))
        {
            query = query.Where((record, model) => model.Slug == modelSlug);
        }

        return await query
            .OrderBy((record, model) => record.TestedAt, OrderByType.Desc)
            .Select((record, model) => new SiteTestRecordRow
            {
                Id = record.Id,
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                TestType = record.TestType,
                Status = record.Status,
                FirstTokenMs = record.FirstTokenMs,
                FullResponseMs = record.FullResponseMs,
                RiskScore = record.RiskScore,
                RiskLevel = record.RiskLevel,
                MatchScore = record.MatchScore,
                ErrorMessage = record.ErrorMessage,
                TestedAt = record.TestedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<PublicSiteLatestTestResponse>> QueryLatestTestsAsync(ulong siteId, string? modelSlug, CancellationToken cancellationToken)
    {
        var query = db.Queryable<TestRecordEntity, AiModelEntity>(
                (record, model) => record.ModelId == model.Id)
            .Where((record, model) =>
                record.SiteId == siteId &&
                model.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(modelSlug))
        {
            query = query.Where((record, model) => model.Slug == modelSlug);
        }

        var rows = await query
            .OrderBy((record, model) => record.TestedAt, OrderByType.Desc)
            .Select((record, model) => new SiteTestRecordRow
            {
                Id = record.Id,
                ModelSlug = model.Slug,
                ModelName = model.DisplayName,
                TestType = record.TestType,
                Status = record.Status,
                FirstTokenMs = record.FirstTokenMs,
                FullResponseMs = record.FullResponseMs,
                RiskScore = record.RiskScore,
                RiskLevel = record.RiskLevel,
                MatchScore = record.MatchScore,
                ErrorMessage = record.ErrorMessage,
                TestedAt = record.TestedAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new PublicSiteLatestTestResponse
        {
            Id = row.Id,
            PublicId = row.Id.ToString(),
            ModelSlug = row.ModelSlug,
            ModelName = row.ModelName,
            TestType = row.TestType,
            Status = row.Status,
            FirstTokenMs = row.FirstTokenMs,
            FullResponseMs = row.FullResponseMs,
            RiskScore = row.RiskScore,
            RiskLevel = row.RiskLevel,
            MatchScore = row.MatchScore,
            ErrorMessage = row.ErrorMessage,
            TestedAt = row.TestedAt
        }).ToList();
    }

    private static PublicSiteStatus24hResponse BuildSiteStatus24h(IReadOnlyList<SiteTestRecordRow> tests)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var buckets = Enumerable.Range(0, 24)
            .Select(index => BuildSiteStatusBucket(
                tests.Where(test =>
                    test.TestedAt.HasValue &&
                    test.TestedAt.Value >= since.AddHours(index) &&
                    test.TestedAt.Value < since.AddHours(index + 1))
                .ToList(),
                since.AddHours(index)))
            .ToList();

        var totalTests = tests.Count;
        var successCount = tests.Count(test => IsSuccessStatus(test.Status));

        return new PublicSiteStatus24hResponse
        {
            SuccessRate = totalTests == 0 ? 0 : Math.Round(successCount * 100m / totalTests, 1),
            TotalTests = totalTests,
            HealthyCount = buckets.Count(bucket => bucket.StatusTone == "success"),
            WarningCount = buckets.Count(bucket => bucket.StatusTone == "warning"),
            CriticalCount = buckets.Count(bucket => bucket.StatusTone == "danger"),
            LastTestedAt = tests.FirstOrDefault()?.TestedAt,
            Buckets = buckets
        };
    }

    private static PublicSiteStatusBucketResponse BuildSiteStatusBucket(IReadOnlyList<SiteTestRecordRow> tests, DateTime slotStartAt)
    {
        var slotLabel = $"{slotStartAt.AddHours(8):HH}:00";
        if (tests.Count == 0)
        {
            return new PublicSiteStatusBucketResponse
            {
                SlotLabel = slotLabel,
                SlotStartAt = slotStartAt,
                StatusTone = "neutral",
                StatusLabel = "暂无测试",
                HasTest = false
            };
        }

        var critical = tests.Where(IsCriticalTest).OrderByDescending(test => test.TestedAt).FirstOrDefault();
        if (critical is not null)
        {
            return MapSiteStatusBucket(critical, slotLabel, slotStartAt, "danger", "异常");
        }

        var warning = tests.Where(IsWarningTest).OrderByDescending(test => test.TestedAt).FirstOrDefault();
        if (warning is not null)
        {
            return MapSiteStatusBucket(warning, slotLabel, slotStartAt, "warning", "波动");
        }

        return MapSiteStatusBucket(tests.OrderByDescending(test => test.TestedAt).First(), slotLabel, slotStartAt, "success", "正常");
    }

    private static PublicSiteStatusBucketResponse MapSiteStatusBucket(SiteTestRecordRow test, string slotLabel, DateTime slotStartAt, string tone, string label)
    {
        return new PublicSiteStatusBucketResponse
        {
            SlotLabel = slotLabel,
            SlotStartAt = slotStartAt,
            StatusTone = tone,
            StatusLabel = label,
            HasTest = true,
            TestedAt = test.TestedAt,
            ModelName = test.ModelName,
            TestType = test.TestType,
            Status = test.Status,
            RiskScore = test.RiskScore
        };
    }

    private static bool IsSuccessStatus(string status)
    {
        return string.Equals(status, "success", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCriticalTest(SiteTestRecordRow test)
    {
        return string.Equals(test.Status, "failed", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(test.Status, "error", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(test.RiskLevel, "high", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(test.RiskLevel, "critical", StringComparison.OrdinalIgnoreCase) ||
               (test.RiskScore ?? 0m) >= 51m;
    }

    private static bool IsWarningTest(SiteTestRecordRow test)
    {
        return string.Equals(test.RiskLevel, "medium", StringComparison.OrdinalIgnoreCase) ||
               ((test.RiskScore ?? 0m) >= 21m && (test.RiskScore ?? 0m) < 51m);
    }

    private static string ResolveRiskLevel(decimal? score)
    {
        var value = score ?? 0m;
        if (value >= 81m) return "critical";
        if (value >= 51m) return "high";
        if (value >= 21m) return "medium";
        return "low";
    }

    private class SiteModelPriceRow
    {
        public string ModelSlug { get; init; } = string.Empty;
        public string ModelName { get; init; } = string.Empty;
        public decimal? EffectiveInputPriceUsd { get; init; }
        public decimal? EffectiveOutputPriceUsd { get; init; }
    }

    private sealed class SiteSnapshotRow : SiteModelPriceRow
    {
        public decimal? AvailabilityScore { get; init; }
        public decimal? StabilityScore { get; init; }
        public decimal? RiskScore { get; init; }
    }

    private sealed class SiteTestRecordRow
    {
        public ulong Id { get; init; }
        public string ModelSlug { get; init; } = string.Empty;
        public string ModelName { get; init; } = string.Empty;
        public string TestType { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public int? FirstTokenMs { get; init; }
        public int? FullResponseMs { get; init; }
        public decimal? RiskScore { get; init; }
        public string RiskLevel { get; init; } = "low";
        public decimal? MatchScore { get; init; }
        public string? ErrorMessage { get; init; }
        public DateTime? TestedAt { get; init; }
    }
}
