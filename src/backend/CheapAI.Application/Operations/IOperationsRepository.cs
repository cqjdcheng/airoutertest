using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.Operations;

public interface IOperationsRepository
{
    Task<PagedResult<RelayOfferListItemResponse>> GetOffersAsync(OperationListQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<TestRecordListItemResponse>> GetTestRecordsAsync(OperationListQuery query, CancellationToken cancellationToken = default);

    Task<TestRecordDetailResponse?> GetTestRecordAsync(ulong id, CancellationToken cancellationToken = default);

    Task<PagedResult<RiskEvidenceListItemResponse>> GetRisksAsync(OperationListQuery query, CancellationToken cancellationToken = default);

    Task<RiskEvidenceDetailResponse?> GetRiskAsync(ulong id, CancellationToken cancellationToken = default);

    Task ReviewRiskAsync(ulong id, string reviewStatus, CancellationToken cancellationToken = default);

    Task<PagedResult<JobExecutionLogListItemResponse>> GetJobLogsAsync(OperationListQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledJobResponse>> GetScheduledJobsAsync(CancellationToken cancellationToken = default);

    Task VerifyOfferAsync(ulong id, CancellationToken cancellationToken = default);

    Task<JobActionResponse> RunManualCrawlAsync(ulong? adminUserId, CancellationToken cancellationToken = default);

    Task<JobActionResponse> RunManualTestAsync(ulong? adminUserId, CancellationToken cancellationToken = default);

    Task<JobActionResponse> RecalculateRisksAsync(ulong? adminUserId, CancellationToken cancellationToken = default);

    Task<JobActionResponse> RebuildRankingsAsync(ulong? adminUserId, CancellationToken cancellationToken = default);
}
