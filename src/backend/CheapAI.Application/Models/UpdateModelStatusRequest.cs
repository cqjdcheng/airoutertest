namespace CheapAI.Application.Models;

public sealed class UpdateModelStatusRequest
{
    public string Status { get; init; } = ModelStatusValue.Active;
}
