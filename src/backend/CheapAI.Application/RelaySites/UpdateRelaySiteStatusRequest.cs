namespace CheapAI.Application.RelaySites;

public sealed class UpdateRelaySiteStatusRequest
{
    public string Status { get; init; } = RelaySiteStatusValue.Draft;
}
