namespace CheapAI.Application.Models;

public sealed class ModelListQuery
{
    public string? Keyword { get; init; }

    public string? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
