using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.Participation;

public sealed class PublicParticipationService(
    IParticipationRepository participationRepository,
    ISelfTestRunner selfTestRunner,
    ISelfTestChallengeService selfTestChallengeService)
{
    public SelfTestChallengeResponse CreateSelfTestChallenge()
    {
        return selfTestChallengeService.CreateChallenge();
    }

    public async Task<SelfTestResponse> CreateSelfTestAsync(CreateSelfTestRequest request, CancellationToken cancellationToken = default)
    {
        if (!selfTestChallengeService.VerifyAndConsume(request.ChallengeId, request.ChallengeAnswer))
        {
            throw new AppUnauthorizedException("Human verification failed or expired.");
        }

        var result = await selfTestRunner.ExecuteAsync(request, cancellationToken);
        return await participationRepository.CreateSelfTestAsync(request, result, cancellationToken);
    }

    public async Task<SelfTestResponse> GetSelfTestAsync(string id, CancellationToken cancellationToken = default)
    {
        return await participationRepository.GetSelfTestAsync(id, cancellationToken)
            ?? throw new AppNotFoundException("Self-test result does not exist or has expired.");
    }

    public Task<ulong> CreateSubmissionAsync(CreateSiteSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        return participationRepository.CreateSubmissionAsync(request, cancellationToken);
    }

    public Task<IReadOnlyList<CapabilityRankingItemResponse>> GetCapabilityRankingAsync(CancellationToken cancellationToken = default)
    {
        return participationRepository.GetCapabilityRankingAsync(cancellationToken);
    }

    public Task<PagedResult<ArticleListItemResponse>> GetArticlesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return participationRepository.GetArticlesAsync(page, pageSize, true, cancellationToken);
    }

    public async Task<ArticleDetailResponse> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await participationRepository.GetArticleBySlugAsync(slug, true, cancellationToken)
            ?? throw new AppNotFoundException("Article does not exist.");
    }
}
