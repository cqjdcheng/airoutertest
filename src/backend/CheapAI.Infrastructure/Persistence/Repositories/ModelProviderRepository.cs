using CheapAI.Application.Common.Paging;
using CheapAI.Application.Models;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class ModelProviderRepository(ISqlSugarClient db) : IModelProviderRepository
{
    public Task<bool> ExistsBySlugAsync(string slug, ulong? excludingId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Queryable<ModelProviderEntity>()
            .Where(x => x.DeletedAt == null && x.Slug == slug);

        if (excludingId.HasValue)
        {
            query = query.Where(x => x.Id != excludingId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModelProviderListItemResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await db.Queryable<ModelProviderEntity>()
            .Where(x => x.DeletedAt == null && x.Status == ModelProviderStatusValue.Active)
            .OrderBy(x => x.SortOrder, OrderByType.Asc)
            .OrderBy(x => x.Id, OrderByType.Desc)
            .Select(x => new ModelProviderListItemResponse
            {
                Id = x.Id,
                Slug = x.Slug,
                Name = x.Name,
                WebsiteUrl = x.WebsiteUrl,
                Description = x.Description,
                Status = x.Status,
                SortOrder = x.SortOrder,
                CreatedAtUtc = x.CreatedAt,
                UpdatedAtUtc = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ModelProviderListItemResponse?> GetByIdAsync(ulong id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Queryable<ModelProviderEntity>()
            .FirstAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<ModelProviderListItemResponse?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var entity = await db.Queryable<ModelProviderEntity>()
            .FirstAsync(x => x.Slug == slug && x.DeletedAt == null, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<PagedResult<ModelProviderListItemResponse>> GetPagedAsync(ModelProviderListQuery query, CancellationToken cancellationToken = default)
    {
        var sqlQuery = db.Queryable<ModelProviderEntity>()
            .Where(x => x.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            sqlQuery = sqlQuery.Where(x =>
                x.Slug.Contains(query.Keyword!) ||
                x.Name.Contains(query.Keyword!));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            sqlQuery = sqlQuery.Where(x => x.Status == query.Status);
        }

        RefAsync<int> total = 0;
        var items = await sqlQuery
            .OrderBy(x => x.SortOrder, OrderByType.Asc)
            .OrderBy(x => x.Id, OrderByType.Desc)
            .Select(x => new ModelProviderListItemResponse
            {
                Id = x.Id,
                Slug = x.Slug,
                Name = x.Name,
                WebsiteUrl = x.WebsiteUrl,
                Description = x.Description,
                Status = x.Status,
                SortOrder = x.SortOrder,
                CreatedAtUtc = x.CreatedAt,
                UpdatedAtUtc = x.UpdatedAt
            })
            .ToPageListAsync(Math.Max(query.Page, 1), Math.Clamp(query.PageSize, 1, 200), total, cancellationToken);

        return new PagedResult<ModelProviderListItemResponse>
        {
            Items = items,
            Page = Math.Max(query.Page, 1),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            Total = total
        };
    }

    public async Task<ulong> InsertAsync(CreateModelProviderRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = new ModelProviderEntity
        {
            Slug = request.Slug,
            Name = request.Name,
            WebsiteUrl = request.WebsiteUrl,
            Description = request.Description,
            Status = request.Status,
            SortOrder = request.SortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };

        return (ulong)await db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public Task UpdateAsync(ulong id, UpdateModelProviderRequest request, CancellationToken cancellationToken = default)
    {
        return db.Updateable<ModelProviderEntity>()
            .SetColumns(x => new ModelProviderEntity
            {
                Slug = request.Slug,
                Name = request.Name,
                WebsiteUrl = request.WebsiteUrl,
                Description = request.Description,
                Status = request.Status,
                SortOrder = request.SortOrder,
                UpdatedAt = DateTime.UtcNow
            })
            .Where(x => x.Id == id && x.DeletedAt == null)
            .ExecuteCommandAsync(cancellationToken);
    }

    public Task UpdateStatusAsync(ulong id, string status, CancellationToken cancellationToken = default)
    {
        return db.Updateable<ModelProviderEntity>()
            .SetColumns(x => new ModelProviderEntity
            {
                Status = status,
                UpdatedAt = DateTime.UtcNow
            })
            .Where(x => x.Id == id && x.DeletedAt == null)
            .ExecuteCommandAsync(cancellationToken);
    }

    public async Task<ModelProviderListItemResponse> EnsureAsync(string slug, string name, CancellationToken cancellationToken = default)
    {
        var existing = await GetBySlugAsync(slug, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var id = await InsertAsync(new CreateModelProviderRequest
        {
            Slug = slug,
            Name = name,
            Status = ModelProviderStatusValue.Active,
            SortOrder = 1000
        }, cancellationToken);

        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to create model provider.");
    }

    private static ModelProviderListItemResponse Map(ModelProviderEntity entity)
    {
        return new ModelProviderListItemResponse
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Name = entity.Name,
            WebsiteUrl = entity.WebsiteUrl,
            Description = entity.Description,
            Status = entity.Status,
            SortOrder = entity.SortOrder,
            CreatedAtUtc = entity.CreatedAt,
            UpdatedAtUtc = entity.UpdatedAt
        };
    }
}
