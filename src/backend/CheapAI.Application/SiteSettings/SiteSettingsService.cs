using CheapAI.Application.Common.Abstractions;

namespace CheapAI.Application.SiteSettings;

public sealed class SiteSettingsService(
    ISiteSettingsRepository siteSettingsRepository,
    ICurrentAdminAccessor currentAdminAccessor)
{
    public Task<SiteSettingsResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        return siteSettingsRepository.GetAsync(cancellationToken);
    }

    public Task UpdateAsync(UpdateSiteSettingsRequest request, CancellationToken cancellationToken = default)
    {
        return siteSettingsRepository.UpdateAsync(request, currentAdminAccessor.AdminUserId, cancellationToken);
    }
}
