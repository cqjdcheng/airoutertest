namespace CheapAI.Application.Public;

public interface IModelRankingSnapshotRepository
{
    Task<HomeOverviewResponse> GetHomeOverviewAsync(CancellationToken cancellationToken = default);

    Task<ModelRankingResponse?> GetModelRankingsAsync(string modelSlug, ModelRankingQuery query, CancellationToken cancellationToken = default);
}
