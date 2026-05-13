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

    [SugarColumn(ColumnName = "test_type")]
    public string TestType { get; set; } = "platform";

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

    [SugarColumn(ColumnName = "tested_at")]
    public DateTime TestedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
