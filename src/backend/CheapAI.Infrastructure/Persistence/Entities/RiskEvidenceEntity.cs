using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("risk_evidences")]
public sealed class RiskEvidenceEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "site_id")]
    public ulong SiteId { get; set; }

    [SugarColumn(ColumnName = "model_id")]
    public ulong ModelId { get; set; }

    [SugarColumn(ColumnName = "test_record_id", IsNullable = true)]
    public ulong? TestRecordId { get; set; }

    [SugarColumn(ColumnName = "rule_code")]
    public string RuleCode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "risk_level")]
    public string RiskLevel { get; set; } = "low";

    [SugarColumn(ColumnName = "risk_score")]
    public decimal RiskScore { get; set; }

    [SugarColumn(ColumnName = "evidence_summary")]
    public string EvidenceSummary { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "evidence_json", IsNullable = true)]
    public string? EvidenceJson { get; set; }

    [SugarColumn(ColumnName = "review_status")]
    public string ReviewStatus { get; set; } = "pending";

    [SugarColumn(ColumnName = "reviewed_at", IsNullable = true)]
    public DateTime? ReviewedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }
}
