namespace CheapAI.Application.Models;

public class ModelListItemResponse
{
    public ulong Id { get; init; }

    public ulong? ProviderId { get; init; }

    public string? ProviderName { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Vendor { get; init; } = string.Empty;

    public string OfficialModelId { get; init; } = string.Empty;

    public string RequestName { get; init; } = string.Empty;

    public string ApiType { get; init; } = ModelApiTypeValue.OpenAi;

    public string DisplayName { get; init; } = string.Empty;

    public string Status { get; init; } = ModelStatusValue.Active;

    public bool IsHot { get; init; }

    public int SortOrder { get; init; } = 1000;

    public decimal? OfficialInputPriceUsd { get; init; }

    public decimal? OfficialOutputPriceUsd { get; init; }

    public decimal? CapabilityScore { get; init; }

    public string? CapabilitySource { get; init; }

    public DateTime? CapabilityUpdatedAtUtc { get; init; }
}
