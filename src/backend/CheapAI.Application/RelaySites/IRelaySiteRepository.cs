using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.RelaySites;

public interface IRelaySiteRepository
{
    Task<PagedResult<RelaySiteListItemResponse>> GetPagedAsync(RelaySiteListQuery query, CancellationToken cancellationToken = default);

    Task<RelaySiteDetailResponse?> GetByIdAsync(ulong id, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(string slug, ulong? excludingId = null, CancellationToken cancellationToken = default);

    Task<ulong> InsertAsync(CreateRelaySiteRequest request, ulong? adminUserId, CancellationToken cancellationToken = default);

    Task UpdateAsync(ulong id, UpdateRelaySiteRequest request, ulong? adminUserId, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(ulong id, string status, ulong? adminUserId, CancellationToken cancellationToken = default);

    Task<long> CountActiveAsync(CancellationToken cancellationToken = default);
}
