namespace CheapAI.Application.Public;

public sealed class ModelRankingQuery
{
    public string RankingType { get; init; } = "price";

    public string Window { get; init; } = "7d";

    public string RiskFilter { get; init; } = "all";

    public bool? SupportsInvoice { get; init; }

    public bool? SupportsRefund { get; init; }

    public bool? HasDocs { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
