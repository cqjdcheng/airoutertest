namespace CheapAI.Application.Models;

public sealed class ModelDetailResponse : ModelListItemResponse
{
    public string? Description { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}
