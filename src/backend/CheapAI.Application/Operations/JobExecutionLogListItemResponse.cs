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

public sealed class ScheduledJobResponse
{
    public string Key { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Rule { get; init; } = string.Empty;

    public string Status { get; init; } = "enabled";

    public DateTime? LastRunAt { get; init; }

    public DateTime? NextRunAt { get; init; }
}
