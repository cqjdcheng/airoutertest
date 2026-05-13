using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("self_tests")]
public sealed class SelfTestEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public string Id { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "site_url")]
    public string SiteUrl { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "model_name")]
    public string ModelName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "test_mode")]
    public string TestMode { get; set; } = "basic";

    [SugarColumn(ColumnName = "is_stream")]
    public bool IsStream { get; set; } = true;

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "succeeded";

    [SugarColumn(ColumnName = "first_token_ms", IsNullable = true)]
    public int? FirstTokenMs { get; set; }

    [SugarColumn(ColumnName = "full_response_ms", IsNullable = true)]
    public int? FullResponseMs { get; set; }

    [SugarColumn(ColumnName = "risk_score")]
    public decimal RiskScore { get; set; }

    [SugarColumn(ColumnName = "risk_level")]
    public string RiskLevel { get; set; } = "low";

    [SugarColumn(ColumnName = "result_summary")]
    public string ResultSummary { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "match_score")]
    public decimal MatchScore { get; set; }

    [SugarColumn(ColumnName = "input_tokens", IsNullable = true)]
    public int? InputTokens { get; set; }

    [SugarColumn(ColumnName = "output_tokens", IsNullable = true)]
    public int? OutputTokens { get; set; }

    [SugarColumn(ColumnName = "total_tokens", IsNullable = true)]
    public int? TotalTokens { get; set; }

    [SugarColumn(ColumnName = "estimated_tokens")]
    public int EstimatedTokens { get; set; }

    [SugarColumn(ColumnName = "tokens_per_second", IsNullable = true)]
    public decimal? TokensPerSecond { get; set; }

    [SugarColumn(ColumnName = "checks_json", IsNullable = true)]
    public string? ChecksJson { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "expires_at")]
    public DateTime ExpiresAt { get; set; }
}
