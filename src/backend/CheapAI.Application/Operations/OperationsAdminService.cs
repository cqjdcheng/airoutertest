using CheapAI.Application.Common.Abstractions;
using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.Operations;

public sealed class OperationsAdminService(IOperationsRepository operationsRepository, ICurrentAdminAccessor currentAdminAccessor)
{
    private static readonly HashSet<string> AllowedRiskReviewStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "pending",
        "confirmed",
        "false_positive",
        "ignored"
    };

    public Task<PagedResult<RelayOfferListItemResponse>> GetOffersAsync(OperationListQuery query, CancellationToken cancellationToken = default)
    {
        return operationsRepository.GetOffersAsync(query, cancellationToken);
    }

    public Task<PagedResult<TestRecordListItemResponse>> GetTestRecordsAsync(OperationListQuery query, CancellationToken cancellationToken = default)
    {
        return operationsRepository.GetTestRecordsAsync(query, cancellationToken);
    }

    public async Task<TestRecordDetailResponse> GetTestRecordAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return await operationsRepository.GetTestRecordAsync(id, cancellationToken)
            ?? throw new AppNotFoundException("Test record does not exist.");
    }

    public Task<PagedResult<RiskEvidenceListItemResponse>> GetRisksAsync(OperationListQuery query, CancellationToken cancellationToken = default)
    {
        return operationsRepository.GetRisksAsync(query, cancellationToken);
    }

    public async Task<RiskEvidenceDetailResponse> GetRiskAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return await operationsRepository.GetRiskAsync(id, cancellationToken)
            ?? throw new AppNotFoundException("Risk evidence does not exist.");
    }

    public Task ReviewRiskAsync(ulong id, ReviewRiskEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        if (!AllowedRiskReviewStatuses.Contains(request.ReviewStatus))
        {
            throw new AppConflictException("Invalid risk review status.");
        }

        return operationsRepository.ReviewRiskAsync(id, request.ReviewStatus.ToLowerInvariant(), cancellationToken);
    }

    public Task<PagedResult<JobExecutionLogListItemResponse>> GetJobLogsAsync(OperationListQuery query, CancellationToken cancellationToken = default)
    {
        return operationsRepository.GetJobLogsAsync(query, cancellationToken);
    }

    public Task<IReadOnlyList<ScheduledJobResponse>> GetScheduledJobsAsync(CancellationToken cancellationToken = default)
    {
        return operationsRepository.GetScheduledJobsAsync(cancellationToken);
    }

    public Task VerifyOfferAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return operationsRepository.VerifyOfferAsync(id, cancellationToken);
    }

    public Task<JobActionResponse> RunManualCrawlAsync(CancellationToken cancellationToken = default)
    {
        return operationsRepository.RunManualCrawlAsync(currentAdminAccessor.AdminUserId, cancellationToken);
    }

    public Task<JobActionResponse> RunManualTestAsync(CancellationToken cancellationToken = default)
    {
        return operationsRepository.RunManualTestAsync(currentAdminAccessor.AdminUserId, cancellationToken);
    }

    public Task<JobActionResponse> RecalculateRisksAsync(CancellationToken cancellationToken = default)
    {
        return operationsRepository.RecalculateRisksAsync(currentAdminAccessor.AdminUserId, cancellationToken);
    }

    public Task<JobActionResponse> RebuildRankingsAsync(CancellationToken cancellationToken = default)
    {
        return operationsRepository.RebuildRankingsAsync(currentAdminAccessor.AdminUserId, cancellationToken);
    }
}
