namespace CheapAI.Application.Public;

public sealed class PublicOverviewService(IModelRankingSnapshotRepository rankingSnapshotRepository)
{
    public Task<HomeOverviewResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        return rankingSnapshotRepository.GetHomeOverviewAsync(cancellationToken);
    }
}
