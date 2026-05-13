namespace CheapAI.Application.Operations;

public sealed class JobExecutionLogListItemResponse
{
    public ulong Id { get; init; }

    public string JobCategory { get; init; } = string.Empty;

    public ulong? JobId { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public DateTime? StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }

    public DateTime CreatedAt { get; init; }
}
