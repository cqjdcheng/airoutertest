namespace CheapAI.Application.RelaySites;

public sealed class RelaySiteOfferQuery
{
    public string? Keyword { get; init; }

    public string? Status { get; init; } = "active";

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
