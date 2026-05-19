using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.Common.Paging;
using CheapAI.Application.RelaySites;

namespace CheapAI.Application.Models;

public sealed class ModelProviderAdminService(IModelProviderRepository modelProviderRepository)
{
    public Task<PagedResult<ModelProviderListItemResponse>> GetPagedAsync(ModelProviderListQuery query, CancellationToken cancellationToken = default)
    {
        return modelProviderRepository.GetPagedAsync(query, cancellationToken);
    }

    public Task<IReadOnlyList<ModelProviderListItemResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return modelProviderRepository.GetAllActiveAsync(cancellationToken);
    }

    public async Task<ModelProviderListItemResponse> GetByIdAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return await modelProviderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppNotFoundException("模型提供商不存在");
    }

    public async Task<ulong> CreateAsync(CreateModelProviderRequest request, CancellationToken cancellationToken = default)
    {
        var slug = SlugHelper.Normalize(request.Slug, request.Name);
        if (await modelProviderRepository.ExistsBySlugAsync(slug, null, cancellationToken))
        {
            throw new AppConflictException("模型提供商 slug 已存在");
        }

        return await modelProviderRepository.InsertAsync(Normalize(request, slug), cancellationToken);
    }

    public async Task UpdateAsync(ulong id, UpdateModelProviderRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await modelProviderRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new AppNotFoundException("模型提供商不存在");
        }

        var slug = SlugHelper.Normalize(request.Slug, request.Name);
        if (await modelProviderRepository.ExistsBySlugAsync(slug, id, cancellationToken))
        {
            throw new AppConflictException("模型提供商 slug 已存在");
        }

        await modelProviderRepository.UpdateAsync(id, Normalize(request, slug), cancellationToken);
    }

    public async Task UpdateStatusAsync(ulong id, UpdateModelProviderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await modelProviderRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new AppNotFoundException("模型提供商不存在");
        }

        await modelProviderRepository.UpdateStatusAsync(id, request.Status, cancellationToken);
    }

    public async Task DeleteAsync(ulong id, CancellationToken cancellationToken = default)
    {
        var existing = await modelProviderRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new AppNotFoundException("模型提供商不存在");
        }

        await modelProviderRepository.DeleteAsync(id, cancellationToken);
    }

    private static UpdateModelProviderRequest Normalize(CreateModelProviderRequest request, string slug)
    {
        return new UpdateModelProviderRequest
        {
            Slug = slug,
            Name = request.Name.Trim(),
            WebsiteUrl = string.IsNullOrWhiteSpace(request.WebsiteUrl) ? null : request.WebsiteUrl.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = request.Status,
            SortOrder = request.SortOrder
        };
    }
}
