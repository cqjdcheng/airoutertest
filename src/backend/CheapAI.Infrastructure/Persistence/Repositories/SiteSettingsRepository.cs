using CheapAI.Application.SiteSettings;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class SiteSettingsRepository(ISqlSugarClient db) : ISiteSettingsRepository
{
    private const byte SingletonId = 1;

    public async Task<SiteSettingsResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var entity = await db.Queryable<SiteSettingEntity>()
            .FirstAsync(x => x.Id == SingletonId, cancellationToken);

        return entity is null ? Default() : Map(entity);
    }

    public async Task UpdateAsync(UpdateSiteSettingsRequest request, ulong? adminUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var siteName = request.SiteName.Trim();
        var siteIconUrl = string.IsNullOrWhiteSpace(request.SiteIconUrl) ? null : request.SiteIconUrl.Trim();

        var existing = await db.Queryable<SiteSettingEntity>()
            .FirstAsync(x => x.Id == SingletonId, cancellationToken);

        if (existing is null)
        {
            await db.Insertable(new SiteSettingEntity
            {
                Id = SingletonId,
                SiteName = siteName,
                SiteIconUrl = siteIconUrl,
                CreatedAt = now,
                UpdatedAt = now,
                UpdatedBy = adminUserId
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        await db.Updateable<SiteSettingEntity>()
            .SetColumns(x => new SiteSettingEntity
            {
                SiteName = siteName,
                SiteIconUrl = siteIconUrl,
                UpdatedAt = now,
                UpdatedBy = adminUserId
            })
            .Where(x => x.Id == SingletonId)
            .ExecuteCommandAsync(cancellationToken);
    }

    private static SiteSettingsResponse Default()
    {
        return new SiteSettingsResponse();
    }

    private static SiteSettingsResponse Map(SiteSettingEntity entity)
    {
        return new SiteSettingsResponse
        {
            SiteName = entity.SiteName,
            SiteIconUrl = entity.SiteIconUrl
        };
    }
}
