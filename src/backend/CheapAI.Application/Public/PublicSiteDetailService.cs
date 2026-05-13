using CheapAI.Application.Common.Exceptions;

namespace CheapAI.Application.Public;

public sealed class PublicSiteDetailService(IPublicSiteQueryRepository publicSiteQueryRepository)
{
    public async Task<SiteDetailResponse> GetAsync(string siteSlug, string? window, string? modelSlug, CancellationToken cancellationToken = default)
    {
        var result = await publicSiteQueryRepository.GetSiteDetailAsync(siteSlug, window, modelSlug, cancellationToken);
        if (result is null)
        {
            throw new AppNotFoundException("站点不存在");
        }

        return result;
    }
}
