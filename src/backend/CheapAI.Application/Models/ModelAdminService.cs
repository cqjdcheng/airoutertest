using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.Common.Paging;
using CheapAI.Application.RelaySites;

namespace CheapAI.Application.Models;

public sealed class ModelAdminService(
    IModelRepository modelRepository,
    IModelProviderRepository modelProviderRepository,
    IModelCatalogCrawler modelCatalogCrawler)
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
        var provider = await ResolveProviderAsync(request, cancellationToken);
        var vendor = ResolveVendor(request, provider);
        var requestName = NormalizeRequestName(request.RequestName, request.OfficialModelId);
        var apiType = NormalizeApiType(request.ApiType, vendor);

        if (await modelRepository.ExistsBySlugAsync(slug, null, cancellationToken))
        {
            throw new AppConflictException("模型 slug 已存在");
        }

        if (await modelRepository.ExistsByVendorAndOfficialModelIdAsync(vendor, request.OfficialModelId, null, cancellationToken))
        {
            throw new AppConflictException("模型 vendor + officialModelId 已存在");
        }

        var normalizedRequest = new CreateModelRequest
        {
            ProviderId = provider?.Id ?? request.ProviderId,
            Slug = slug,
            Vendor = vendor,
            OfficialModelId = request.OfficialModelId,
            RequestName = requestName,
            ApiType = apiType,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Status = request.Status,
            IsHot = request.IsHot,
            SortOrder = request.SortOrder,
            OfficialInputPriceUsd = request.OfficialInputPriceUsd,
            OfficialOutputPriceUsd = request.OfficialOutputPriceUsd,
            CapabilityScore = request.CapabilityScore,
            CapabilitySource = request.CapabilitySource
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
        var provider = await ResolveProviderAsync(request, cancellationToken);
        var vendor = ResolveVendor(request, provider);
        var requestName = NormalizeRequestName(request.RequestName, request.OfficialModelId);
        var apiType = NormalizeApiType(request.ApiType, vendor);

        if (await modelRepository.ExistsBySlugAsync(slug, id, cancellationToken))
        {
            throw new AppConflictException("模型 slug 已存在");
        }

        if (await modelRepository.ExistsByVendorAndOfficialModelIdAsync(vendor, request.OfficialModelId, id, cancellationToken))
        {
            throw new AppConflictException("模型 vendor + officialModelId 已存在");
        }

        var normalizedRequest = new UpdateModelRequest
        {
            ProviderId = provider?.Id ?? request.ProviderId,
            Slug = slug,
            Vendor = vendor,
            OfficialModelId = request.OfficialModelId,
            RequestName = requestName,
            ApiType = apiType,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Status = request.Status,
            IsHot = request.IsHot,
            SortOrder = request.SortOrder,
            OfficialInputPriceUsd = request.OfficialInputPriceUsd,
            OfficialOutputPriceUsd = request.OfficialOutputPriceUsd,
            CapabilityScore = request.CapabilityScore,
            CapabilitySource = request.CapabilitySource
        };

        await modelRepository.UpdateAsync(id, normalizedRequest, cancellationToken);
    }

    public Task<IReadOnlyList<ModelImportPreviewItemResponse>> PreviewImportAsync(ModelImportPreviewRequest request, CancellationToken cancellationToken = default)
    {
        return modelCatalogCrawler.PreviewAsync(request, cancellationToken);
    }

    public Task<IReadOnlyList<ModelImportPreviewItemResponse>> PreviewOpenRouterImportAsync(CancellationToken cancellationToken = default)
    {
        return modelCatalogCrawler.PreviewOpenRouterAsync(cancellationToken);
    }

    public async Task<ImportModelsResponse> ImportAsync(ImportModelsRequest request, CancellationToken cancellationToken = default)
    {
        var created = 0;
        var updated = 0;

        foreach (var item in request.Models)
        {
            var provider = await ResolveProviderAsync(item, cancellationToken);
            var vendor = ResolveVendor(item, provider);
            var requestName = NormalizeRequestName(item.RequestName, item.OfficialModelId);
            var apiType = NormalizeApiType(item.ApiType, vendor);
            var existing = await modelRepository.GetByVendorAndOfficialModelIdAsync(vendor, item.OfficialModelId, cancellationToken);
            var slugFallback = item.OfficialModelId.Contains('/', StringComparison.Ordinal)
                ? item.OfficialModelId
                : $"{vendor}-{item.OfficialModelId}";
            var slug = SlugHelper.Normalize(item.Slug, slugFallback);
            var modelRequest = new UpdateModelRequest
            {
                ProviderId = provider?.Id ?? item.ProviderId,
                Slug = slug,
                Vendor = vendor,
                OfficialModelId = item.OfficialModelId,
                RequestName = requestName,
                ApiType = apiType,
                DisplayName = item.DisplayName,
                Description = item.Description,
                Status = item.Status,
                IsHot = item.IsHot,
                SortOrder = item.SortOrder,
                OfficialInputPriceUsd = item.OfficialInputPriceUsd,
                OfficialOutputPriceUsd = item.OfficialOutputPriceUsd,
                CapabilityScore = item.CapabilityScore,
                CapabilitySource = item.CapabilitySource
            };

            if (existing is null)
            {
                await CreateAsync(modelRequest, cancellationToken);
                created++;
                continue;
            }

            await UpdateAsync(existing.Id, modelRequest, cancellationToken);
            updated++;
        }

        return new ImportModelsResponse
        {
            CreatedCount = created,
            UpdatedCount = updated
        };
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

    public async Task UpdateMetadataAsync(ulong id, UpdateModelMetadataRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await modelRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new AppNotFoundException("妯″瀷涓嶅瓨鍦?");
        }

        await modelRepository.UpdateMetadataAsync(id, request, cancellationToken);
    }

    private async Task<ModelProviderListItemResponse?> ResolveProviderAsync(CreateModelRequest request, CancellationToken cancellationToken)
    {
        return await ResolveProviderAsync(request.ProviderId, request.ProviderSlug, request.ProviderName, request.Vendor, cancellationToken);
    }

    private async Task<ModelProviderListItemResponse?> ResolveProviderAsync(ImportModelItemRequest request, CancellationToken cancellationToken)
    {
        return await ResolveProviderAsync(request.ProviderId, request.ProviderSlug, request.ProviderName, request.Vendor, cancellationToken);
    }

    private async Task<ModelProviderListItemResponse?> ResolveProviderAsync(
        ulong? providerId,
        string? providerSlug,
        string? providerName,
        string? vendor,
        CancellationToken cancellationToken)
    {
        if (providerId.HasValue)
        {
            return await modelProviderRepository.GetByIdAsync(providerId.Value, cancellationToken)
                ?? throw new AppNotFoundException("模型提供商不存在");
        }

        if (string.IsNullOrWhiteSpace(providerSlug) && string.IsNullOrWhiteSpace(providerName))
        {
            return null;
        }

        var resolvedName = FirstNonEmpty(providerName, vendor, providerSlug, "Unknown");
        var resolvedSlug = SlugHelper.Normalize(providerSlug, resolvedName);
        return await modelProviderRepository.EnsureAsync(resolvedSlug, resolvedName, cancellationToken);
    }

    private static string ResolveVendor(CreateModelRequest request, ModelProviderListItemResponse? provider)
    {
        return FirstNonEmpty(provider?.Name, request.ProviderName, request.Vendor, request.ProviderSlug, "Unknown");
    }

    private static string ResolveVendor(ImportModelItemRequest request, ModelProviderListItemResponse? provider)
    {
        return FirstNonEmpty(provider?.Name, request.ProviderName, request.Vendor, request.ProviderSlug, "Unknown");
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private static string NormalizeRequestName(string? requestName, string officialModelId)
    {
        var normalized = FirstNonEmpty(requestName, officialModelId);
        if (normalized.Contains('/', StringComparison.Ordinal))
        {
            normalized = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? normalized;
        }

        return normalized.Trim().TrimStart('~');
    }

    private static string NormalizeApiType(string? apiType, string vendor)
    {
        if (!string.IsNullOrWhiteSpace(apiType) && ModelApiTypeValue.All.Contains(apiType.Trim().ToLowerInvariant()))
        {
            return apiType.Trim().ToLowerInvariant();
        }

        return vendor.Contains("anthropic", StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains("claude", StringComparison.OrdinalIgnoreCase)
                ? ModelApiTypeValue.Anthropic
                : ModelApiTypeValue.OpenAi;
    }
}
