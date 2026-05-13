namespace CheapAI.Application.Common.Paging;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long Total { get; init; }
}
