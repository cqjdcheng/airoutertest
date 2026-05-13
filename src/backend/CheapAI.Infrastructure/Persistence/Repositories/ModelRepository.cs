using CheapAI.Application.Common.Paging;
using CheapAI.Application.Models;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class ModelRepository(ISqlSugarClient db) : IModelRepository
{
    public async Task<long> CountActiveAsync(CancellationToken cancellationToken = default)
    {
        return await db.Queryable<AiModelEntity>()
            .Where(x => x.DeletedAt == null && x.Status == ModelStatusValue.Active)
            .CountAsync(cancellationToken);
    }

    public Task<bool> ExistsBySlugAsync(string slug, ulong? excludingId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Queryable<AiModelEntity>()
            .Where(x => x.DeletedAt == null && x.Slug == slug);

        if (excludingId.HasValue)
        {
            query = query.Where(x => x.Id != excludingId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> ExistsByVendorAndOfficialModelIdAsync(string vendor, string officialModelId, ulong? excludingId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Queryable<AiModelEntity>()
            .Where(x => x.DeletedAt == null && x.Vendor == vendor && x.OfficialModelId == officialModelId);

        if (excludingId.HasValue)
        {
            query = query.Where(x => x.Id != excludingId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<ModelDetailResponse?> GetByIdAsync(ulong id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Queryable<AiModelEntity>()
            .FirstAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken);

        return entity is null ? null : MapDetail(entity);
    }

    public async Task<ModelListItemResponse?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var entity = await db.Queryable<AiModelEntity>()
            .FirstAsync(x => x.Slug == slug && x.DeletedAt == null, cancellationToken);

        return entity is null ? null : MapListItem(entity);
    }

    public async Task<PagedResult<ModelListItemResponse>> GetPagedAsync(ModelListQuery query, CancellationToken cancellationToken = default)
    {
        var sqlQuery = db.Queryable<AiModelEntity>()
            .Where(x => x.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            sqlQuery = sqlQuery.Where(x =>
                x.Slug.Contains(query.Keyword!) ||
                x.DisplayName.Contains(query.Keyword!) ||
                x.OfficialModelId.Contains(query.Keyword!));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            sqlQuery = sqlQuery.Where(x => x.Status == query.Status);
        }

        RefAsync<int> total = 0;
        var items = await sqlQuery
            .OrderBy(x => x.Id, OrderByType.Desc)
            .Select(x => new ModelListItemResponse
            {
                Id = x.Id,
                Slug = x.Slug,
                Vendor = x.Vendor,
                OfficialModelId = x.OfficialModelId,
                DisplayName = x.DisplayName,
                Status = x.Status,
                OfficialInputPriceUsd = x.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = x.OfficialOutputPriceUsd
            })
            .ToPageListAsync(query.Page, query.PageSize, total, cancellationToken);

        return new PagedResult<ModelListItemResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            Total = total
        };
    }

    public async Task<ulong> InsertAsync(CreateModelRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new AiModelEntity
        {
            Slug = request.Slug,
            Vendor = request.Vendor,
            OfficialModelId = request.OfficialModelId,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Status = request.Status,
            OfficialInputPriceUsd = request.OfficialInputPriceUsd,
            OfficialOutputPriceUsd = request.OfficialOutputPriceUsd,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return (ulong)await db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public Task UpdateAsync(ulong id, UpdateModelRequest request, CancellationToken cancellationToken = default)
    {
        return db.Updateable<AiModelEntity>()
            .SetColumns(x => new AiModelEntity
            {
                Slug = request.Slug,
                Vendor = request.Vendor,
                OfficialModelId = request.OfficialModelId,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Status = request.Status,
                OfficialInputPriceUsd = request.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = request.OfficialOutputPriceUsd,
                UpdatedAt = DateTime.UtcNow
            })
            .Where(x => x.Id == id && x.DeletedAt == null)
            .ExecuteCommandAsync(cancellationToken);
    }

    public Task UpdateStatusAsync(ulong id, string status, CancellationToken cancellationToken = default)
    {
        return db.Updateable<AiModelEntity>()
            .SetColumns(x => new AiModelEntity
            {
                Status = status,
                UpdatedAt = DateTime.UtcNow
            })
            .Where(x => x.Id == id && x.DeletedAt == null)
            .ExecuteCommandAsync(cancellationToken);
    }

    private static ModelListItemResponse MapListItem(AiModelEntity entity)
    {
        return new ModelListItemResponse
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Vendor = entity.Vendor,
            OfficialModelId = entity.OfficialModelId,
            DisplayName = entity.DisplayName,
            Status = entity.Status,
            OfficialInputPriceUsd = entity.OfficialInputPriceUsd,
            OfficialOutputPriceUsd = entity.OfficialOutputPriceUsd
        };
    }

    private static ModelDetailResponse MapDetail(AiModelEntity entity)
    {
        return new ModelDetailResponse
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Vendor = entity.Vendor,
            OfficialModelId = entity.OfficialModelId,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            Status = entity.Status,
            OfficialInputPriceUsd = entity.OfficialInputPriceUsd,
            OfficialOutputPriceUsd = entity.OfficialOutputPriceUsd,
            CreatedAtUtc = entity.CreatedAt,
            UpdatedAtUtc = entity.UpdatedAt
        };
    }
}
