namespace CheapAI.Infrastructure.Jobs;

public sealed class HangfireOptions
{
    public const string SectionName = "CheapAi:Hangfire";

    public string DashboardPath { get; init; } = "/hangfire";

    public bool RunServer { get; init; }
}
