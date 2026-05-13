namespace CheapAI.Application.Models;

public class ModelListItemResponse
{
    public ulong Id { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Vendor { get; init; } = string.Empty;

    public string OfficialModelId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Status { get; init; } = ModelStatusValue.Active;

    public decimal? OfficialInputPriceUsd { get; init; }

    public decimal? OfficialOutputPriceUsd { get; init; }
}
