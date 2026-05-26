namespace CheapAI.Application.Operations;

public sealed class OperationListQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Keyword { get; init; }
}
