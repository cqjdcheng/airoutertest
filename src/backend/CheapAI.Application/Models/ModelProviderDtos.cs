using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.Models;

public sealed class ModelProviderListQuery
{
    public string? Keyword { get; init; }

    public string? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed class ModelProviderListItemResponse
{
    public ulong Id { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? WebsiteUrl { get; init; }

    public string? Description { get; init; }

    public string Status { get; init; } = ModelProviderStatusValue.Active;

    public int SortOrder { get; init; } = 1000;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

public class CreateModelProviderRequest
{
    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? WebsiteUrl { get; init; }

    public string? Description { get; init; }

    public string Status { get; init; } = ModelProviderStatusValue.Active;

    public int SortOrder { get; init; } = 1000;
}

public sealed class UpdateModelProviderRequest : CreateModelProviderRequest;

public sealed class UpdateModelProviderStatusRequest
{
    public string Status { get; init; } = ModelProviderStatusValue.Active;
}

public interface IModelProviderRepository
{
    Task<PagedResult<ModelProviderListItemResponse>> GetPagedAsync(ModelProviderListQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModelProviderListItemResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task<ModelProviderListItemResponse?> GetByIdAsync(ulong id, CancellationToken cancellationToken = default);

    Task<ModelProviderListItemResponse?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(string slug, ulong? excludingId = null, CancellationToken cancellationToken = default);

    Task<ulong> InsertAsync(CreateModelProviderRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(ulong id, UpdateModelProviderRequest request, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(ulong id, string status, CancellationToken cancellationToken = default);

    Task<ModelProviderListItemResponse> EnsureAsync(string slug, string name, CancellationToken cancellationToken = default);
}
