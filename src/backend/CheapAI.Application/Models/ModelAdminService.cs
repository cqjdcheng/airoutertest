using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.Common.Paging;
using CheapAI.Application.RelaySites;

namespace CheapAI.Application.Models;

public sealed class ModelAdminService(IModelRepository modelRepository)
{
    public Task<PagedResult<ModelListItemResponse>> GetPagedAsync(ModelListQuery query, CancellationToken cancellationToken = default)
    {
        return modelRepository.GetPagedAsync(query, cancellationToken);
    }

    public async Task<ModelDetailResponse> GetByIdAsync(ulong id, CancellationToken cancellationToken = default)
    {
        var result = await modelRepository.GetByIdAsync(id, cancellationToken);
        if (result is null)
        {
            throw new AppNotFoundException("模型不存在");
        }

        return result;
    }

    public async Task<ulong> CreateAsync(CreateModelRequest request, CancellationToken cancellationToken = default)
    {
        var slug = SlugHelper.Normalize(request.Slug, request.DisplayName);
        if (await modelRepository.ExistsBySlugAsync(slug, null, cancellationToken))
        {
            throw new AppConflictException("模型 slug 已存在");
        }

        if (await modelRepository.ExistsByVendorAndOfficialModelIdAsync(request.Vendor, request.OfficialModelId, null, cancellationToken))
        {
            throw new AppConflictException("模型 vendor + officialModelId 已存在");
        }

        var normalizedRequest = new CreateModelRequest
        {
            Slug = slug,
            Vendor = request.Vendor,
            OfficialModelId = request.OfficialModelId,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Status = request.Status,
            OfficialInputPriceUsd = request.OfficialInputPriceUsd,
            OfficialOutputPriceUsd = request.OfficialOutputPriceUsd
        };

        return await modelRepository.InsertAsync(normalizedRequest, cancellationToken);
    }

    public async Task UpdateAsync(ulong id, UpdateModelRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await modelRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new AppNotFoundException("模型不存在");
        }

        var slug = SlugHelper.Normalize(request.Slug, request.DisplayName);
        if (await modelRepository.ExistsBySlugAsync(slug, id, cancellationToken))
        {
            throw new AppConflictException("模型 slug 已存在");
        }

        if (await modelRepository.ExistsByVendorAndOfficialModelIdAsync(request.Vendor, request.OfficialModelId, id, cancellationToken))
        {
            throw new AppConflictException("模型 vendor + officialModelId 已存在");
        }

        var normalizedRequest = new UpdateModelRequest
        {
            Slug = slug,
            Vendor = request.Vendor,
            OfficialModelId = request.OfficialModelId,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Status = request.Status,
            OfficialInputPriceUsd = request.OfficialInputPriceUsd,
            OfficialOutputPriceUsd = request.OfficialOutputPriceUsd
        };

        await modelRepository.UpdateAsync(id, normalizedRequest, cancellationToken);
    }

    public async Task UpdateStatusAsync(ulong id, UpdateModelStatusRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await modelRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new AppNotFoundException("模型不存在");
        }

        await modelRepository.UpdateStatusAsync(id, request.Status, cancellationToken);
    }
}
