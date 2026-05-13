namespace CheapAI.Application.Public;

public interface IPublicSiteQueryRepository
{
    Task<SiteDetailResponse?> GetSiteDetailAsync(string siteSlug, string? window, string? modelSlug, CancellationToken cancellationToken = default);
}
