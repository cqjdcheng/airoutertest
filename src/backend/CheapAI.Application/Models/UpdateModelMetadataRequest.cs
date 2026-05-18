namespace CheapAI.Application.Models;

public sealed class UpdateModelMetadataRequest
{
    public bool IsHot { get; init; }

    public int SortOrder { get; init; } = 1000;
}
