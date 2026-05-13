namespace CheapAI.Application.Operations;

public sealed class RiskEvidenceDetailResponse : RiskEvidenceListItemResponse
{
    public ulong? TestRecordId { get; init; }

    public string? EvidenceJson { get; init; }

    public DateTime? ReviewedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
