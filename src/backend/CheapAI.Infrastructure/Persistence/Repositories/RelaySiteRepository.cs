using CheapAI.Application.Common.Paging;
using CheapAI.Application.Models;
using CheapAI.Application.Scoring;
using CheapAI.Application.RelaySites;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class RelaySiteRepository(ISqlSugarClient db) : IRelaySiteRepository
{
    public async Task<long> CountActiveAsync(CancellationToken cancellationToken = default)
    {
        return await db.Queryable<RelaySiteEntity>()
            .Where(x => x.DeletedAt == null && x.Status == RelaySiteStatusValue.Active)
            .CountAsync(cancellationToken);
    }

    public Task<bool> ExistsBySlugAsync(string slug, ulong? excludingId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Queryable<RelaySiteEntity>()
            .Where(x => x.DeletedAt == null && x.Slug == slug);

        if (excludingId.HasValue)
        {
            query = query.Where(x => x.Id != excludingId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<RelaySiteDetailResponse?> GetByIdAsync(ulong id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Queryable<RelaySiteEntity>()
            .FirstAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var offers = await LoadOffersAsync(id, cancellationToken);
        var recentTests = await LoadRecentTestsAsync(id, cancellationToken);
        return MapDetail(entity, offers, recentTests);
    }

    public async Task<PagedResult<RelaySiteListItemResponse>> GetPagedAsync(RelaySiteListQuery query, CancellationToken cancellationToken = default)
    {
        var sqlQuery = db.Queryable<RelaySiteEntity>()
            .Where(x => x.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            sqlQuery = sqlQuery.Where(x => x.Name.Contains(query.Keyword!) || x.Slug.Contains(query.Keyword!));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            sqlQuery = sqlQuery.Where(x => x.Status == query.Status);
        }

        RefAsync<int> total = 0;
        var items = await sqlQuery
            .OrderBy(x => x.Id, OrderByType.Desc)
            .Select(x => new RelaySiteListItemResponse
            {
                Id = x.Id,
                Slug = x.Slug,
                Name = x.Name,
                BaseUrl = x.BaseUrl,
                Status = x.Status,
                SupportsRefund = x.SupportsRefund,
                SupportsInvoice = x.SupportsInvoice,
                HasDocs = x.HasDocs,
                CreatedAtUtc = x.CreatedAt
            })
            .ToPageListAsync(query.Page, query.PageSize, total, cancellationToken);

        return new PagedResult<RelaySiteListItemResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            Total = total
        };
    }

    public async Task<ulong> InsertAsync(CreateRelaySiteRequest request, ulong? adminUserId, CancellationToken cancellationToken = default)
    {
        var entity = new RelaySiteEntity
        {
            Slug = request.Slug,
            Name = request.Name,
            BaseUrl = request.BaseUrl,
            WebsiteUrl = request.WebsiteUrl,
            Description = request.Description,
            SupportsRefund = request.SupportsRefund,
            SupportsInvoice = request.SupportsInvoice,
            HasDocs = request.HasDocs,
            DocsUrl = request.DocsUrl,
            InviteUrl = request.InviteUrl,
            RecentReview = request.RecentReview,
            Status = RelaySiteStatusValue.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = adminUserId,
            UpdatedBy = adminUserId
        };

        try
        {
            db.Ado.BeginTran();
            var siteId = (ulong)await db.Insertable(entity).ExecuteReturnBigIdentityAsync();
            await UpsertOffersCoreAsync(siteId, request.Offers, archiveMissing: false, cancellationToken);
            db.Ado.CommitTran();
            return siteId;
        }
        catch
        {
            db.Ado.RollbackTran();
            throw;
        }
    }

    public async Task UpdateAsync(ulong id, UpdateRelaySiteRequest request, ulong? adminUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            db.Ado.BeginTran();
            await db.Updateable<RelaySiteEntity>()
                .SetColumns(x => new RelaySiteEntity
                {
                    Slug = request.Slug,
                    Name = request.Name,
                    BaseUrl = request.BaseUrl,
                    WebsiteUrl = request.WebsiteUrl,
                    Description = request.Description,
                    SupportsRefund = request.SupportsRefund,
                    SupportsInvoice = request.SupportsInvoice,
                    HasDocs = request.HasDocs,
                    DocsUrl = request.DocsUrl,
                    InviteUrl = request.InviteUrl,
                    RecentReview = request.RecentReview,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = adminUserId
                })
                .Where(x => x.Id == id && x.DeletedAt == null)
                .ExecuteCommandAsync(cancellationToken);

            await UpsertOffersCoreAsync(id, request.Offers, archiveMissing: true, cancellationToken);
            db.Ado.CommitTran();
        }
        catch
        {
            db.Ado.RollbackTran();
            throw;
        }
    }

    public Task UpdateStatusAsync(ulong id, string status, ulong? adminUserId, CancellationToken cancellationToken = default)
    {
        return db.Updateable<RelaySiteEntity>()
            .SetColumns(x => new RelaySiteEntity
            {
                Status = status,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            })
            .Where(x => x.Id == id && x.DeletedAt == null)
            .ExecuteCommandAsync(cancellationToken);
    }

    public Task DeleteAsync(ulong id, ulong? adminUserId, CancellationToken cancellationToken = default)
    {
        return db.Updateable<RelaySiteEntity>()
            .SetColumns(x => new RelaySiteEntity
            {
                Status = RelaySiteStatusValue.Archived,
                DeletedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            })
            .Where(x => x.Id == id && x.DeletedAt == null)
            .ExecuteCommandAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<RelaySiteOfferResponse>> LoadOffersAsync(ulong siteId, CancellationToken cancellationToken)
    {
        return await db.Queryable<RelayOfferEntity, AiModelEntity>(
                (offer, model) => offer.ModelId == model.Id)
            .Where((offer, model) => offer.SiteId == siteId && offer.Status != "archived" && model.DeletedAt == null)
            .OrderBy((offer, model) => offer.SourceType, OrderByType.Asc)
            .OrderBy((offer, model) => model.SortOrder, OrderByType.Asc)
            .OrderBy((offer, model) => offer.UpdatedAt, OrderByType.Desc)
            .Select((offer, model) => new RelaySiteOfferResponse
            {
                Id = offer.Id,
                ModelId = model.Id,
                ModelSlug = model.Slug,
                Vendor = model.Vendor,
                OfficialModelId = model.OfficialModelId,
                RequestName = model.RequestName,
                ApiType = model.ApiType,
                DisplayName = model.DisplayName,
                OfficialInputPriceUsd = offer.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = offer.OfficialOutputPriceUsd,
                SiteInputPriceUsd = offer.SiteInputPriceUsd,
                SiteOutputPriceUsd = offer.SiteOutputPriceUsd,
                EffectiveInputPriceUsd = offer.EffectiveInputPriceUsd,
                EffectiveOutputPriceUsd = offer.EffectiveOutputPriceUsd,
                RechargeRatio = offer.RechargeRatio,
                BonusRatio = offer.BonusRatio,
                SourceType = offer.SourceType,
                Status = offer.Status,
                CrawledAt = offer.CrawledAt,
                ReviewedAt = offer.ReviewedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<RelaySiteTestRecordResponse>> LoadRecentTestsAsync(ulong siteId, CancellationToken cancellationToken)
    {
        return await db.Queryable<TestRecordEntity, AiModelEntity>(
                (record, model) => record.ModelId == model.Id)
            .Where((record, model) => record.SiteId == siteId && model.DeletedAt == null)
            .OrderBy((record, model) => record.TestedAt, OrderByType.Desc)
            .Select((record, model) => new RelaySiteTestRecordResponse
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
                ErrorMessage = record.ErrorMessage,
                TestedAt = record.TestedAt
            })
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    private async Task UpsertOffersCoreAsync(
        ulong siteId,
        IReadOnlyList<RelaySiteOfferUpsertRequest> offers,
        bool archiveMissing,
        CancellationToken cancellationToken)
    {
        var touchedOfferIds = new List<ulong>();

        foreach (var offer in offers)
        {
            var modelId = await ResolveModelIdAsync(offer, cancellationToken);
            var now = DateTime.UtcNow;
            var rechargeRatio = offer.RechargeRatio <= 0 ? 1 : offer.RechargeRatio;
            var entity = new RelayOfferEntity
            {
                SiteId = siteId,
                ModelId = modelId,
                SourceType = string.IsNullOrWhiteSpace(offer.SourceType) ? "manual" : offer.SourceType.Trim(),
                Currency = "USD",
                OfficialInputPriceUsd = offer.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = offer.OfficialOutputPriceUsd,
                SiteInputPriceUsd = offer.SiteInputPriceUsd,
                SiteOutputPriceUsd = offer.SiteOutputPriceUsd,
                RechargeRatio = rechargeRatio,
                BonusRatio = offer.BonusRatio,
                EffectiveInputPriceUsd = CalculateEffective(offer.SiteInputPriceUsd, rechargeRatio, offer.BonusRatio),
                EffectiveOutputPriceUsd = CalculateEffective(offer.SiteOutputPriceUsd, rechargeRatio, offer.BonusRatio),
                Status = string.IsNullOrWhiteSpace(offer.Status) ? "active" : offer.Status,
                ReviewedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            var existing = await db.Queryable<RelayOfferEntity>()
                .FirstAsync(x =>
                    x.SiteId == siteId &&
                    x.ModelId == modelId &&
                    x.SourceType == entity.SourceType,
                    cancellationToken);

            if (existing is null)
            {
                var insertedId = (ulong)await db.Insertable(entity).ExecuteReturnBigIdentityAsync();
                touchedOfferIds.Add(insertedId);
                continue;
            }

            entity.Id = existing.Id;
            entity.CreatedAt = existing.CreatedAt;
            entity.CrawledAt = existing.CrawledAt;
            await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            touchedOfferIds.Add(existing.Id);
        }

        if (!archiveMissing)
        {
            return;
        }

        var update = db.Updateable<RelayOfferEntity>()
            .SetColumns(x => new RelayOfferEntity
            {
                Status = "archived",
                UpdatedAt = DateTime.UtcNow
            })
            .Where(x => x.SiteId == siteId);

        if (touchedOfferIds.Count > 0)
        {
            update = update.Where(x => !touchedOfferIds.Contains(x.Id));
        }

        await update.ExecuteCommandAsync(cancellationToken);
    }

    private async Task<ulong> ResolveModelIdAsync(RelaySiteOfferUpsertRequest offer, CancellationToken cancellationToken)
    {
        if (offer.ModelId is > 0)
        {
            return offer.ModelId.Value;
        }

        var vendor = string.IsNullOrWhiteSpace(offer.Vendor) ? "Custom" : offer.Vendor.Trim();
        var officialModelId = offer.OfficialModelId?.Trim() ?? string.Empty;
        var requestName = NormalizeRequestName(offer.RequestName, officialModelId);
        var apiType = NormalizeApiType(offer.ApiType, vendor, requestName);
        var existing = await db.Queryable<AiModelEntity>()
            .FirstAsync(x =>
                x.DeletedAt == null &&
                (x.RequestName == requestName || x.OfficialModelId == officialModelId),
                cancellationToken);

        if (existing is not null)
        {
            await db.Updateable<AiModelEntity>()
                .SetColumns(x => new AiModelEntity
                {
                    DisplayName = string.IsNullOrWhiteSpace(offer.DisplayName) ? existing.DisplayName : offer.DisplayName.Trim(),
                    RequestName = string.IsNullOrWhiteSpace(existing.RequestName) ? requestName : existing.RequestName,
                    ApiType = string.IsNullOrWhiteSpace(existing.ApiType) ? apiType : existing.ApiType,
                    OfficialInputPriceUsd = offer.OfficialInputPriceUsd ?? existing.OfficialInputPriceUsd,
                    OfficialOutputPriceUsd = offer.OfficialOutputPriceUsd ?? existing.OfficialOutputPriceUsd,
                    UpdatedAt = DateTime.UtcNow
                })
                .Where(x => x.Id == existing.Id)
                .ExecuteCommandAsync(cancellationToken);
            return existing.Id;
        }

        var displayName = string.IsNullOrWhiteSpace(offer.DisplayName) ? requestName : offer.DisplayName.Trim();
        var slug = await ResolveUniqueModelSlugAsync(
            SlugHelper.Normalize(offer.ModelSlug, $"{vendor}-{requestName}"),
            cancellationToken);
        var now = DateTime.UtcNow;
        return (ulong)await db.Insertable(new AiModelEntity
        {
            Slug = slug,
            Vendor = vendor,
            OfficialModelId = string.IsNullOrWhiteSpace(officialModelId) ? requestName : officialModelId,
            RequestName = requestName,
            ApiType = apiType,
            DisplayName = displayName,
            Description = "由中转站录入页自动创建。",
            Status = "active",
            SortOrder = 1000,
            OfficialInputPriceUsd = offer.OfficialInputPriceUsd,
            OfficialOutputPriceUsd = offer.OfficialOutputPriceUsd,
            CreatedAt = now,
            UpdatedAt = now
        }).ExecuteReturnBigIdentityAsync();
    }

    private async Task<string> ResolveUniqueModelSlugAsync(string slug, CancellationToken cancellationToken)
    {
        if (!await db.Queryable<AiModelEntity>().AnyAsync(x => x.Slug == slug && x.DeletedAt == null, cancellationToken))
        {
            return slug;
        }

        var suffix = 2;
        while (true)
        {
            var candidate = $"{slug}-{suffix++}";
            if (!await db.Queryable<AiModelEntity>().AnyAsync(x => x.Slug == candidate && x.DeletedAt == null, cancellationToken))
            {
                return candidate;
            }
        }
    }

    private static decimal? CalculateEffective(decimal? price, decimal rechargeRatio, decimal bonusRatio)
    {
        return price.HasValue ? PriceCalculator.CalculateEffectiveUsd(price.Value, rechargeRatio, bonusRatio) : null;
    }

    private static string NormalizeRequestName(string? requestName, string officialModelId)
    {
        var normalized = string.IsNullOrWhiteSpace(requestName) ? officialModelId : requestName.Trim();
        if (normalized.Contains('/', StringComparison.Ordinal))
        {
            normalized = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? normalized;
        }

        return normalized.TrimStart('~');
    }

    private static string NormalizeApiType(string? apiType, string vendor, string requestName)
    {
        if (!string.IsNullOrWhiteSpace(apiType) && ModelApiTypeValue.All.Contains(apiType.Trim().ToLowerInvariant()))
        {
            return apiType.Trim().ToLowerInvariant();
        }

        var source = $"{vendor} {requestName}";
        return source.Contains("anthropic", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("claude", StringComparison.OrdinalIgnoreCase)
                ? ModelApiTypeValue.Anthropic
                : ModelApiTypeValue.OpenAi;
    }

    private static RelaySiteDetailResponse MapDetail(
        RelaySiteEntity entity,
        IReadOnlyList<RelaySiteOfferResponse> offers,
        IReadOnlyList<RelaySiteTestRecordResponse> recentTests)
    {
        return new RelaySiteDetailResponse
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Name = entity.Name,
            BaseUrl = entity.BaseUrl,
            WebsiteUrl = entity.WebsiteUrl,
            Description = entity.Description,
            SupportsRefund = entity.SupportsRefund,
            SupportsInvoice = entity.SupportsInvoice,
            HasDocs = entity.HasDocs,
            DocsUrl = entity.DocsUrl,
            Status = entity.Status,
            InviteUrl = entity.InviteUrl,
            RecentReview = entity.RecentReview,
            CreatedAtUtc = entity.CreatedAt,
            UpdatedAtUtc = entity.UpdatedAt,
            Offers = offers,
            RecentTests = recentTests
        };
    }
}
