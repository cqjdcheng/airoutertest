using CheapAI.Application.Common.Paging;
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

        return entity is null ? null : MapDetail(entity);
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

        return (ulong)await db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public Task UpdateAsync(ulong id, UpdateRelaySiteRequest request, ulong? adminUserId, CancellationToken cancellationToken = default)
    {
        return db.Updateable<RelaySiteEntity>()
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

    private static RelaySiteDetailResponse MapDetail(RelaySiteEntity entity)
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
            UpdatedAtUtc = entity.UpdatedAt
        };
    }
}
