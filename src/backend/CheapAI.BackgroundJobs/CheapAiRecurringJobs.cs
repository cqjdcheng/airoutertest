using CheapAI.Application.Operations;

namespace CheapAI.BackgroundJobs;

public sealed class CheapAiRecurringJobs(IOperationsRepository operationsRepository)
{
    public Task RunPriceCrawlAsync(CancellationToken cancellationToken = default)
    {
        return operationsRepository.RunManualCrawlAsync(null, cancellationToken);
    }

    public Task RunPlatformTestAsync(CancellationToken cancellationToken = default)
    {
        return operationsRepository.RunManualTestAsync(null, cancellationToken);
    }

    public Task RunRankingRebuildAsync(CancellationToken cancellationToken = default)
    {
        return operationsRepository.RebuildRankingsAsync(null, cancellationToken);
    }
}
