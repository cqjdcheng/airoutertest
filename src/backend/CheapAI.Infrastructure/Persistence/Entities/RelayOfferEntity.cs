using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("relay_offers")]
public sealed class RelayOfferEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "site_id")]
    public ulong SiteId { get; set; }

    [SugarColumn(ColumnName = "model_id")]
    public ulong ModelId { get; set; }

    [SugarColumn(ColumnName = "channel_id", IsNullable = true)]
    public ulong? ChannelId { get; set; }

    [SugarColumn(ColumnName = "source_type")]
    public string SourceType { get; set; } = "manual";

    [SugarColumn(ColumnName = "currency")]
    public string Currency { get; set; } = "USD";

    [SugarColumn(ColumnName = "official_input_price_usd", IsNullable = true)]
    public decimal? OfficialInputPriceUsd { get; set; }

    [SugarColumn(ColumnName = "official_output_price_usd", IsNullable = true)]
    public decimal? OfficialOutputPriceUsd { get; set; }

    [SugarColumn(ColumnName = "site_input_price_usd", IsNullable = true)]
    public decimal? SiteInputPriceUsd { get; set; }

    [SugarColumn(ColumnName = "site_output_price_usd", IsNullable = true)]
    public decimal? SiteOutputPriceUsd { get; set; }

    [SugarColumn(ColumnName = "recharge_ratio")]
    public decimal RechargeRatio { get; set; } = 1;

    [SugarColumn(ColumnName = "bonus_ratio")]
    public decimal BonusRatio { get; set; }

    [SugarColumn(ColumnName = "effective_input_price_usd", IsNullable = true)]
    public decimal? EffectiveInputPriceUsd { get; set; }

    [SugarColumn(ColumnName = "effective_output_price_usd", IsNullable = true)]
    public decimal? EffectiveOutputPriceUsd { get; set; }

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "active";

    [SugarColumn(ColumnName = "auto_test_enabled")]
    public bool AutoTestEnabled { get; set; }

    [SugarColumn(ColumnName = "test_api_key", IsNullable = true)]
    public string? TestApiKey { get; set; }

    [SugarColumn(ColumnName = "crawled_at", IsNullable = true)]
    public DateTime? CrawledAt { get; set; }

    [SugarColumn(ColumnName = "reviewed_at", IsNullable = true)]
    public DateTime? ReviewedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }
}
