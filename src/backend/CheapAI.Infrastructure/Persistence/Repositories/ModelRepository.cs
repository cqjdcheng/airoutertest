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
        return await db.Queryable<AiModelEntity, ModelProviderEntity>(
                (model, provider) => new JoinQueryInfos(JoinType.Left, model.ProviderId == provider.Id))
            .Where((model, provider) => model.Id == id && model.DeletedAt == null)
            .Select((model, provider) => new ModelDetailResponse
            {
                Id = model.Id,
                ProviderId = model.ProviderId,
                ProviderName = provider.Name,
                Slug = model.Slug,
                Vendor = model.Vendor,
                OfficialModelId = model.OfficialModelId,
                RequestName = model.RequestName,
                ApiType = model.ApiType,
                DisplayName = model.DisplayName,
                Description = model.Description,
                Status = model.Status,
                IsHot = model.IsHot,
                SortOrder = model.SortOrder,
                OfficialInputPriceUsd = model.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = model.OfficialOutputPriceUsd,
                CapabilityScore = model.CapabilityScore,
                CapabilitySource = model.CapabilitySource,
                CapabilityUpdatedAtUtc = model.CapabilityUpdatedAt,
                CreatedAtUtc = model.CreatedAt,
                UpdatedAtUtc = model.UpdatedAt
            })
            .FirstAsync(cancellationToken);
    }

    public async Task<ModelListItemResponse?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await db.Queryable<AiModelEntity, ModelProviderEntity>(
                (model, provider) => new JoinQueryInfos(JoinType.Left, model.ProviderId == provider.Id))
            .Where((model, provider) => model.Slug == slug && model.DeletedAt == null)
            .Select((model, provider) => new ModelListItemResponse
            {
                Id = model.Id,
                ProviderId = model.ProviderId,
                ProviderName = provider.Name,
                Slug = model.Slug,
                Vendor = model.Vendor,
                OfficialModelId = model.OfficialModelId,
                RequestName = model.RequestName,
                ApiType = model.ApiType,
                DisplayName = model.DisplayName,
                Status = model.Status,
                IsHot = model.IsHot,
                SortOrder = model.SortOrder,
                OfficialInputPriceUsd = model.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = model.OfficialOutputPriceUsd,
                CapabilityScore = model.CapabilityScore,
                CapabilitySource = model.CapabilitySource,
                CapabilityUpdatedAtUtc = model.CapabilityUpdatedAt
            })
            .FirstAsync(cancellationToken);
    }

    public async Task<ModelDetailResponse?> GetByVendorAndOfficialModelIdAsync(string vendor, string officialModelId, CancellationToken cancellationToken = default)
    {
        return await db.Queryable<AiModelEntity, ModelProviderEntity>(
                (model, provider) => new JoinQueryInfos(JoinType.Left, model.ProviderId == provider.Id))
            .Where((model, provider) =>
                model.Vendor == vendor &&
                model.OfficialModelId == officialModelId &&
                model.DeletedAt == null)
            .Select((model, provider) => new ModelDetailResponse
            {
                Id = model.Id,
                ProviderId = model.ProviderId,
                ProviderName = provider.Name,
                Slug = model.Slug,
                Vendor = model.Vendor,
                OfficialModelId = model.OfficialModelId,
                RequestName = model.RequestName,
                ApiType = model.ApiType,
                DisplayName = model.DisplayName,
                Description = model.Description,
                Status = model.Status,
                IsHot = model.IsHot,
                SortOrder = model.SortOrder,
                OfficialInputPriceUsd = model.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = model.OfficialOutputPriceUsd,
                CapabilityScore = model.CapabilityScore,
                CapabilitySource = model.CapabilitySource,
                CapabilityUpdatedAtUtc = model.CapabilityUpdatedAt,
                CreatedAtUtc = model.CreatedAt,
                UpdatedAtUtc = model.UpdatedAt
            })
            .FirstAsync(cancellationToken);
    }

    public async Task<PagedResult<ModelListItemResponse>> GetPagedAsync(ModelListQuery query, CancellationToken cancellationToken = default)
    {
        var sqlQuery = db.Queryable<AiModelEntity, ModelProviderEntity>(
                (model, provider) => new JoinQueryInfos(JoinType.Left, model.ProviderId == provider.Id))
            .Where((model, provider) => model.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            sqlQuery = sqlQuery.Where((model, provider) =>
                model.Slug.Contains(query.Keyword!) ||
                model.DisplayName.Contains(query.Keyword!) ||
                model.OfficialModelId.Contains(query.Keyword!) ||
                model.RequestName.Contains(query.Keyword!) ||
                model.ApiType.Contains(query.Keyword!) ||
                model.Vendor.Contains(query.Keyword!) ||
                provider.Name.Contains(query.Keyword!));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            sqlQuery = sqlQuery.Where((model, provider) => model.Status == query.Status);
        }

        RefAsync<int> total = 0;
        var items = await sqlQuery
            .OrderBy((model, provider) => model.SortOrder, OrderByType.Asc)
            .OrderBy((model, provider) => model.Id, OrderByType.Desc)
            .Select((model, provider) => new ModelListItemResponse
            {
                Id = model.Id,
                ProviderId = model.ProviderId,
                ProviderName = provider.Name,
                Slug = model.Slug,
                Vendor = model.Vendor,
                OfficialModelId = model.OfficialModelId,
                RequestName = model.RequestName,
                ApiType = model.ApiType,
                DisplayName = model.DisplayName,
                Status = model.Status,
                IsHot = model.IsHot,
                SortOrder = model.SortOrder,
                OfficialInputPriceUsd = model.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = model.OfficialOutputPriceUsd,
                CapabilityScore = model.CapabilityScore,
                CapabilitySource = model.CapabilitySource,
                CapabilityUpdatedAtUtc = model.CapabilityUpdatedAt
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
            ProviderId = request.ProviderId,
            Slug = request.Slug,
            Vendor = request.Vendor,
            OfficialModelId = request.OfficialModelId,
            RequestName = request.RequestName,
            ApiType = request.ApiType,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Status = request.Status,
            IsHot = request.IsHot,
            SortOrder = request.SortOrder,
            OfficialInputPriceUsd = request.OfficialInputPriceUsd,
            OfficialOutputPriceUsd = request.OfficialOutputPriceUsd,
            CapabilityScore = request.CapabilityScore,
            CapabilitySource = request.CapabilitySource,
            CapabilityUpdatedAt = request.CapabilityScore.HasValue ? DateTime.UtcNow : null,
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
                ProviderId = request.ProviderId,
                Slug = request.Slug,
                Vendor = request.Vendor,
                OfficialModelId = request.OfficialModelId,
                RequestName = request.RequestName,
                ApiType = request.ApiType,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Status = request.Status,
                IsHot = request.IsHot,
                SortOrder = request.SortOrder,
                OfficialInputPriceUsd = request.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = request.OfficialOutputPriceUsd,
                CapabilityScore = request.CapabilityScore,
                CapabilitySource = request.CapabilitySource,
                CapabilityUpdatedAt = request.CapabilityScore.HasValue ? DateTime.UtcNow : null,
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

    public Task UpdateMetadataAsync(ulong id, UpdateModelMetadataRequest request, CancellationToken cancellationToken = default)
    {
        return db.Updateable<AiModelEntity>()
            .SetColumns(x => new AiModelEntity
            {
                IsHot = request.IsHot,
                SortOrder = request.SortOrder,
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
            ProviderId = entity.ProviderId,
            Slug = entity.Slug,
            Vendor = entity.Vendor,
            OfficialModelId = entity.OfficialModelId,
            RequestName = entity.RequestName,
            ApiType = entity.ApiType,
            DisplayName = entity.DisplayName,
            Status = entity.Status,
            IsHot = entity.IsHot,
            SortOrder = entity.SortOrder,
            OfficialInputPriceUsd = entity.OfficialInputPriceUsd,
            OfficialOutputPriceUsd = entity.OfficialOutputPriceUsd,
            CapabilityScore = entity.CapabilityScore,
            CapabilitySource = entity.CapabilitySource,
            CapabilityUpdatedAtUtc = entity.CapabilityUpdatedAt
        };
    }

    private static ModelDetailResponse MapDetail(AiModelEntity entity)
    {
        return new ModelDetailResponse
        {
            Id = entity.Id,
            ProviderId = entity.ProviderId,
            Slug = entity.Slug,
            Vendor = entity.Vendor,
            OfficialModelId = entity.OfficialModelId,
            RequestName = entity.RequestName,
            ApiType = entity.ApiType,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            Status = entity.Status,
            IsHot = entity.IsHot,
            SortOrder = entity.SortOrder,
            OfficialInputPriceUsd = entity.OfficialInputPriceUsd,
            OfficialOutputPriceUsd = entity.OfficialOutputPriceUsd,
            CapabilityScore = entity.CapabilityScore,
            CapabilitySource = entity.CapabilitySource,
            CapabilityUpdatedAtUtc = entity.CapabilityUpdatedAt,
            CreatedAtUtc = entity.CreatedAt,
            UpdatedAtUtc = entity.UpdatedAt
        };
    }
}
