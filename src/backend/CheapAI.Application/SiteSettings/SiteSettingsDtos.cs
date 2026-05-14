namespace CheapAI.Application.SiteSettings;

public sealed class SiteSettingsResponse
{
    public string SiteName { get; init; } = "CheapAI";

    public string? SiteIconUrl { get; init; }
}

public sealed class UpdateSiteSettingsRequest
{
    public string SiteName { get; init; } = "CheapAI";

    public string? SiteIconUrl { get; init; }
}

public interface ISiteSettingsRepository
{
    Task<SiteSettingsResponse> GetAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateSiteSettingsRequest request, ulong? adminUserId, CancellationToken cancellationToken = default);
}
