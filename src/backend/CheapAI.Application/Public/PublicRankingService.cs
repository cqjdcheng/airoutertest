using CheapAI.Application.Common.Exceptions;

namespace CheapAI.Application.Public;

public sealed class PublicRankingService(IModelRankingSnapshotRepository rankingSnapshotRepository)
{
    public async Task<ModelRankingResponse> GetAsync(string modelSlug, ModelRankingQuery query, CancellationToken cancellationToken = default)
    {
        var result = await rankingSnapshotRepository.GetModelRankingsAsync(modelSlug, query, cancellationToken);
        if (result is null)
        {
            throw new AppNotFoundException("模型不存在");
        }

        return result;
    }
}
