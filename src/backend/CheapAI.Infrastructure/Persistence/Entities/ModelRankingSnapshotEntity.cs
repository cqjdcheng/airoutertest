using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("model_ranking_snapshots")]
public sealed class ModelRankingSnapshotEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "ranking_type")]
    public string RankingType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "window_type")]
    public string WindowType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "model_id")]
    public ulong ModelId { get; set; }

    [SugarColumn(ColumnName = "site_id")]
    public ulong SiteId { get; set; }

    [SugarColumn(ColumnName = "rank_position")]
    public int RankPosition { get; set; }

    [SugarColumn(ColumnName = "effective_input_price_usd", IsNullable = true)]
    public decimal? EffectiveInputPriceUsd { get; set; }

    [SugarColumn(ColumnName = "effective_output_price_usd", IsNullable = true)]
    public decimal? EffectiveOutputPriceUsd { get; set; }

    [SugarColumn(ColumnName = "availability_score", IsNullable = true)]
    public decimal? AvailabilityScore { get; set; }

    [SugarColumn(ColumnName = "stability_score", IsNullable = true)]
    public decimal? StabilityScore { get; set; }

    [SugarColumn(ColumnName = "speed_score", IsNullable = true)]
    public decimal? SpeedScore { get; set; }

    [SugarColumn(ColumnName = "risk_score", IsNullable = true)]
    public decimal? RiskScore { get; set; }

    [SugarColumn(ColumnName = "final_score", IsNullable = true)]
    public decimal? FinalScore { get; set; }

    [SugarColumn(ColumnName = "snapshot_at")]
    public DateTime SnapshotAt { get; set; }
}
