using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.Common.Extensions;

public static class QueryExtensions
{
    public static PagedResult<T> ToPagedResult<T>(this IReadOnlyList<T> items, int page, int pageSize, long total)
    {
        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }
}
