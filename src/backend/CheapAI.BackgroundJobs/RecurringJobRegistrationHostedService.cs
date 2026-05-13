using Hangfire;

namespace CheapAI.BackgroundJobs;

public sealed class RecurringJobRegistrationHostedService(
    IRecurringJobManager recurringJobManager,
    ILogger<RecurringJobRegistrationHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        recurringJobManager.AddOrUpdate<CheapAiRecurringJobs>(
            "cheapai-price-crawl-hourly",
            job => job.RunPriceCrawlAsync(CancellationToken.None),
            Cron.Hourly);

        recurringJobManager.AddOrUpdate<CheapAiRecurringJobs>(
            "cheapai-platform-test-hourly",
            job => job.RunPlatformTestAsync(CancellationToken.None),
            Cron.Hourly);

        recurringJobManager.AddOrUpdate<CheapAiRecurringJobs>(
            "cheapai-risk-recalculation-hourly",
            job => job.RunRiskRecalculationAsync(CancellationToken.None),
            Cron.Hourly);

        recurringJobManager.AddOrUpdate<CheapAiRecurringJobs>(
            "cheapai-ranking-rebuild-hourly",
            job => job.RunRankingRebuildAsync(CancellationToken.None),
            Cron.Hourly);

        logger.LogInformation("CheapAI recurring jobs registered.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
