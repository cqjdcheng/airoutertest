namespace CheapAI.Application.Operations;

public sealed class JobActionResponse
{
    public string JobCategory { get; init; } = string.Empty;

    public ulong? JobId { get; init; }

    public string Status { get; init; } = "succeeded";

    public int AffectedCount { get; init; }

    public string Message { get; init; } = string.Empty;
}
