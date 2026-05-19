using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.Participation;

public sealed class AdminParticipationService(IParticipationRepository participationRepository)
{
    public Task<PagedResult<SiteSubmissionListItemResponse>> GetSubmissionsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return participationRepository.GetSubmissionsAsync(page, pageSize, cancellationToken);
    }

    public Task ApproveSubmissionAsync(ulong id, ReviewSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        return participationRepository.ReviewSubmissionAsync(id, "approved", request.ReviewNote, cancellationToken);
    }

    public Task RejectSubmissionAsync(ulong id, ReviewSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        return participationRepository.ReviewSubmissionAsync(id, "rejected", request.ReviewNote, cancellationToken);
    }

    public Task<PagedResult<ArticleListItemResponse>> GetArticlesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return participationRepository.GetArticlesAsync(page, pageSize, false, cancellationToken);
    }

    public async Task<ArticleDetailResponse> GetArticleByIdAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return await participationRepository.GetArticleByIdAsync(id, cancellationToken)
            ?? throw new AppNotFoundException("文章不存在");
    }

    public Task<ulong> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default)
    {
        return participationRepository.CreateArticleAsync(request, cancellationToken);
    }

    public Task UpdateArticleAsync(ulong id, UpdateArticleRequest request, CancellationToken cancellationToken = default)
    {
        return participationRepository.UpdateArticleAsync(id, request, cancellationToken);
    }

    public Task PublishArticleAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return participationRepository.UpdateArticleStatusAsync(id, "published", cancellationToken);
    }

    public Task ArchiveArticleAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return participationRepository.UpdateArticleStatusAsync(id, "archived", cancellationToken);
    }

    public Task DeleteArticleAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return participationRepository.DeleteArticleAsync(id, cancellationToken);
    }
}
