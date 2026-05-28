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
            .OrderBy((offer, model) => model.SortOrder, OrderByType.Asc)
            .OrderBy((offer, model) => model.Id, OrderByType.Desc)
            .Select((offer, model) => new SiteModelPriceRow
            {
                ModelSlug = model.Slug,
                ModelName = model.DisplayName
            })
            .ToListAsync(cancellationToken);

        var snapshotQuery = db.Queryable<ModelRankingSnapshotEntity, AiModelEntity>(
                (snapshot, model) => snapshot.ModelId == model.Id)
            .Where((snapshot, model) =>
                snapshot.SiteId == site.Id &&
                snapshot.RankingType == "stability" &&
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
                ModelName = x.ModelName
            }).ToList();

        var dedupedRows = modelPriceRows
            .GroupBy(x => x.ModelSlug)
            .Select(x => x.First())
            .ToList();

        var recentStatusTests = await QueryRecentTestsAsync(site.Id, modelSlug, DateTime.UtcNow.Date.AddDays(-29), cancellationToken);
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
            Pricing = [],
            LatestTests = latestTests,
            Status24h = BuildSiteStatus24h(recentStatusTests),
            RiskSummary = new PublicSiteRiskSummaryResponse
            {
                MaxRiskScore = maxRiskScore,
                RiskLevel = ResolveRiskLevel(maxRiskScore)
            },
            Trends = new PublicSiteTrendsResponse
            {
                Price = [],
                Stability =
                [
                    new PublicTrendPointResponse { Label = rankingWindow, Value = snapshotRows.FirstOrDefault()?.StabilityScore ?? 0m }
                ]
            }
        };
    }

    private async Task<List<SiteTestRecordRow>> QueryRecentTestsAsync(ulong siteId, string? modelSlug, DateTime since, CancellationToken cancellationToken)
    {
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
                ModelId = record.ModelId,
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
                ModelId = record.ModelId,
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
        var windowTests = tests
            .Where(test => test.TestedAt.HasValue && test.TestedAt.Value >= since)
            .ToList();
        var buckets = Enumerable.Range(0, 24)
            .Select(index => BuildSiteStatusBucket(
                windowTests.Where(test =>
                    test.TestedAt.HasValue &&
                    test.TestedAt.Value >= since.AddHours(index) &&
                    test.TestedAt.Value < since.AddHours(index + 1))
                .ToList(),
                since.AddHours(index)))
            .ToList();

        var totalTests = windowTests.Count;
        var successCount = windowTests.Count(test => IsSuccessStatus(test.Status));
        var scores = windowTests.Select(ResolveTestScore).ToList();

        return new PublicSiteStatus24hResponse
        {
            SuccessRate = totalTests == 0 ? 0 : Math.Round(successCount * 100m / totalTests, 1),
            TotalTests = totalTests,
            TestedModelCount = windowTests.Where(test => test.ModelId > 0).Select(test => test.ModelId).Distinct().Count(),
            AverageScore = scores.Count == 0 ? null : Math.Round(scores.Average(), 1),
            HealthyCount = buckets.Count(bucket => bucket.StatusTone == "success"),
            WarningCount = buckets.Count(bucket => bucket.StatusTone == "warning"),
            CriticalCount = buckets.Count(bucket => bucket.StatusTone == "danger"),
            LastTestedAt = windowTests.OrderByDescending(test => test.TestedAt).FirstOrDefault()?.TestedAt,
            Buckets = buckets,
            DailyBuckets = BuildSiteStatusDailyBuckets(tests)
        };
    }

    private static IReadOnlyList<PublicSiteStatusBucketResponse> BuildSiteStatusDailyBuckets(IReadOnlyList<SiteTestRecordRow> tests)
    {
        var today = DateTime.UtcNow.Date;
        var since = today.AddDays(-29);
        return Enumerable.Range(0, 30)
            .Select(index =>
            {
                var dayStart = since.AddDays(index);
                var dayTests = tests
                    .Where(test =>
                        test.TestedAt.HasValue &&
                        test.TestedAt.Value >= dayStart &&
                        test.TestedAt.Value < dayStart.AddDays(1))
                    .ToList();
                return BuildSiteStatusBucket(dayTests, dayStart, dayStart.AddHours(8).ToString("MM/dd"));
            })
            .ToList();
    }

    private static PublicSiteStatusBucketResponse BuildSiteStatusBucket(
        IReadOnlyList<SiteTestRecordRow> tests,
        DateTime slotStartAt,
        string? slotLabelOverride = null)
    {
        var slotLabel = slotLabelOverride ?? $"{slotStartAt.AddHours(8):HH}:00";
        if (tests.Count == 0)
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

        var totalTests = tests.Count;
        var successCount = tests.Count(test => IsSuccessStatus(test.Status));
        var averageScore = Math.Round(tests.Select(ResolveTestScore).Average(), 1);
        var latestTestedAt = tests
            .Where(test => test.TestedAt.HasValue)
            .Max(test => test.TestedAt);

        return new PublicSiteStatusBucketResponse
        {
            SlotLabel = slotLabel,
            SlotStartAt = slotStartAt,
            StatusTone = ResolveScoreTone(averageScore),
            StatusLabel = ResolveScoreLabel(averageScore),
            HasTest = true,
            TotalTests = totalTests,
            SuccessCount = successCount,
            SuccessRate = totalTests == 0 ? 0 : Math.Round(successCount * 100m / totalTests, 1),
            TestedModelCount = tests.Where(test => test.ModelId > 0).Select(test => test.ModelId).Distinct().Count(),
            AverageScore = averageScore,
            TestedAt = latestTestedAt
        };
    }

    private static bool IsSuccessStatus(string status)
    {
        return string.Equals(status, "success", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase);
    }

    private static decimal ResolveTestScore(SiteTestRecordRow test)
    {
        if (!IsSuccessStatus(test.Status))
        {
            return 0;
        }

        var score = test.MatchScore is > 0 ? test.MatchScore.Value : 100m - (test.RiskScore ?? 0m);
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
        public ulong ModelId { get; init; }
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
