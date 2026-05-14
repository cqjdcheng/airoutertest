using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("test_records")]
public sealed class TestRecordEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "site_id")]
    public ulong SiteId { get; set; }

    [SugarColumn(ColumnName = "model_id")]
    public ulong ModelId { get; set; }

    [SugarColumn(ColumnName = "channel_id", IsNullable = true)]
    public ulong? ChannelId { get; set; }

    [SugarColumn(ColumnName = "site_url", IsNullable = true)]
    public string? SiteUrl { get; set; }

    [SugarColumn(ColumnName = "site_name", IsNullable = true)]
    public string? SiteName { get; set; }

    [SugarColumn(ColumnName = "model_slug", IsNullable = true)]
    public string? ModelSlug { get; set; }

    [SugarColumn(ColumnName = "model_name", IsNullable = true)]
    public string? ModelName { get; set; }

    [SugarColumn(ColumnName = "test_type")]
    public string TestType { get; set; } = "platform";

    [SugarColumn(ColumnName = "is_stream")]
    public bool IsStream { get; set; } = true;

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "success";

    [SugarColumn(ColumnName = "first_token_ms", IsNullable = true)]
    public int? FirstTokenMs { get; set; }

    [SugarColumn(ColumnName = "full_response_ms", IsNullable = true)]
    public int? FullResponseMs { get; set; }

    [SugarColumn(ColumnName = "error_code", IsNullable = true)]
    public string? ErrorCode { get; set; }

    [SugarColumn(ColumnName = "error_message", IsNullable = true)]
    public string? ErrorMessage { get; set; }

    [SugarColumn(ColumnName = "prompt_hash", IsNullable = true)]
    public string? PromptHash { get; set; }

    [SugarColumn(ColumnName = "response_hash", IsNullable = true)]
    public string? ResponseHash { get; set; }

    [SugarColumn(ColumnName = "detected_model_id", IsNullable = true)]
    public string? DetectedModelId { get; set; }

    [SugarColumn(ColumnName = "risk_score")]
    public decimal RiskScore { get; set; }

    [SugarColumn(ColumnName = "risk_level")]
    public string RiskLevel { get; set; } = "low";

    [SugarColumn(ColumnName = "result_summary", IsNullable = true)]
    public string? ResultSummary { get; set; }

    [SugarColumn(ColumnName = "match_score")]
    public decimal MatchScore { get; set; }

    [SugarColumn(ColumnName = "input_tokens", IsNullable = true)]
    public int? InputTokens { get; set; }

    [SugarColumn(ColumnName = "output_tokens", IsNullable = true)]
    public int? OutputTokens { get; set; }

    [SugarColumn(ColumnName = "total_tokens", IsNullable = true)]
    public int? TotalTokens { get; set; }

    [SugarColumn(ColumnName = "estimated_tokens")]
    public int EstimatedTokens { get; set; } = 1000;

    [SugarColumn(ColumnName = "tokens_per_second", IsNullable = true)]
    public decimal? TokensPerSecond { get; set; }

    [SugarColumn(ColumnName = "checks_json", IsNullable = true)]
    public string? ChecksJson { get; set; }

    [SugarColumn(ColumnName = "self_test_id", IsNullable = true)]
    public string? SelfTestId { get; set; }

    [SugarColumn(ColumnName = "tested_at")]
    public DateTime TestedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
