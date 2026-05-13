using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.Models;

public interface IModelRepository
{
    Task<PagedResult<ModelListItemResponse>> GetPagedAsync(ModelListQuery query, CancellationToken cancellationToken = default);

    Task<ModelDetailResponse?> GetByIdAsync(ulong id, CancellationToken cancellationToken = default);

    Task<ModelListItemResponse?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> ExistsByVendorAndOfficialModelIdAsync(string vendor, string officialModelId, ulong? excludingId = null, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(string slug, ulong? excludingId = null, CancellationToken cancellationToken = default);

    Task<ulong> InsertAsync(CreateModelRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(ulong id, UpdateModelRequest request, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(ulong id, string status, CancellationToken cancellationToken = default);

    Task<long> CountActiveAsync(CancellationToken cancellationToken = default);
}
