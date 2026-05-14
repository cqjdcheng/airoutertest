using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.Public;

public sealed class PublicCatalogService(IPublicCatalogRepository publicCatalogRepository)
{
    public Task<PagedResult<PublicTestRecordListItemResponse>> GetLatestTestsAsync(int page, int pageSize, string? testType = null, CancellationToken cancellationToken = default)
    {
        return publicCatalogRepository.GetLatestTestsAsync(page, Math.Clamp(pageSize, 1, 200), testType, cancellationToken);
    }

    public async Task<PublicTestRecordDetailResponse> GetTestDetailAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return await publicCatalogRepository.GetTestDetailAsync(id, cancellationToken)
            ?? throw new Common.Exceptions.AppNotFoundException("Test record does not exist.");
    }

    public Task<PagedResult<PublicRelaySiteRankingItemResponse>> GetRelaySitesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return publicCatalogRepository.GetRelaySitesAsync(page, Math.Clamp(pageSize, 1, 100), cancellationToken);
    }

    public Task<PagedResult<PublicModelCatalogItemResponse>> GetModelsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return publicCatalogRepository.GetModelsAsync(page, Math.Clamp(pageSize, 1, 100), cancellationToken);
    }

    public Task<IReadOnlyList<PublicCheapestRankingResponse>> GetCheapestRankingsAsync(IReadOnlyList<string> modelSlugs, int limit, CancellationToken cancellationToken = default)
    {
        return publicCatalogRepository.GetCheapestRankingsAsync(modelSlugs, Math.Clamp(limit, 1, 20), cancellationToken);
    }
}
