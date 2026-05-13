namespace CheapAI.Infrastructure;

public sealed class InfrastructureRuntimeState
{
    public bool HangfireEnabled { get; set; }

    public string? HangfireDisabledReason { get; set; }
}
