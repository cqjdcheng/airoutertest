namespace CheapAI.Application.Operations;

public sealed class RelayOfferListItemResponse
{
    public ulong Id { get; init; }

    public string SiteName { get; init; } = string.Empty;

    public string SiteSlug { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string ModelSlug { get; init; } = string.Empty;

    public decimal? OfficialInputPriceUsd { get; init; }

    public decimal? OfficialOutputPriceUsd { get; init; }

    public decimal? SiteInputPriceUsd { get; init; }

    public decimal? SiteOutputPriceUsd { get; init; }

    public decimal? EffectiveInputPriceUsd { get; init; }

    public decimal? EffectiveOutputPriceUsd { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime? CrawledAt { get; init; }

    public DateTime? ReviewedAt { get; init; }
}
