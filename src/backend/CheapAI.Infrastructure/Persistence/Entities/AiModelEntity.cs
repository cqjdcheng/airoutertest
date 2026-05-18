using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("models")]
public sealed class AiModelEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "provider_id", IsNullable = true)]
    public ulong? ProviderId { get; set; }

    [SugarColumn(ColumnName = "slug")]
    public string Slug { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "vendor")]
    public string Vendor { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "official_model_id")]
    public string OfficialModelId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "request_name")]
    public string RequestName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "api_type")]
    public string ApiType { get; set; } = "openai";

    [SugarColumn(ColumnName = "display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "description", IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "active";

    [SugarColumn(ColumnName = "is_hot")]
    public bool IsHot { get; set; }

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; } = 1000;

    [SugarColumn(ColumnName = "official_input_price_usd", IsNullable = true)]
    public decimal? OfficialInputPriceUsd { get; set; }

    [SugarColumn(ColumnName = "official_output_price_usd", IsNullable = true)]
    public decimal? OfficialOutputPriceUsd { get; set; }

    [SugarColumn(ColumnName = "capability_score", IsNullable = true)]
    public decimal? CapabilityScore { get; set; }

    [SugarColumn(ColumnName = "capability_source", IsNullable = true)]
    public string? CapabilitySource { get; set; }

    [SugarColumn(ColumnName = "capability_updated_at", IsNullable = true)]
    public DateTime? CapabilityUpdatedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }

    [SugarColumn(ColumnName = "deleted_at", IsNullable = true)]
    public DateTime? DeletedAt { get; set; }
}
