using Quartz;

namespace CheapAI.BackgroundJobs;

public sealed class PriceCrawlQuartzJob(CheapAiRecurringJobs jobs) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        return jobs.RunPriceCrawlAsync(context.CancellationToken);
    }
}

public sealed class AutoTestQuartzJob(CheapAiRecurringJobs jobs) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        return jobs.RunPlatformTestAsync(context.CancellationToken);
    }
}

public sealed class RankingRebuildQuartzJob(CheapAiRecurringJobs jobs) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        return jobs.RunRankingRebuildAsync(context.CancellationToken);
    }
}
