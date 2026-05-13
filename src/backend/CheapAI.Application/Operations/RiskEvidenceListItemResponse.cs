namespace CheapAI.Application.Operations;

public class RiskEvidenceListItemResponse
{
    public ulong Id { get; init; }

    public string SiteName { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string RuleCode { get; init; } = string.Empty;

    public string RiskLevel { get; init; } = string.Empty;

    public decimal RiskScore { get; init; }

    public string EvidenceSummary { get; init; } = string.Empty;

    public string ReviewStatus { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}
