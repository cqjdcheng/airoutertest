using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("model_capability_snapshots")]
public sealed class ModelCapabilitySnapshotEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "model_id")]
    public ulong ModelId { get; set; }

    [SugarColumn(ColumnName = "source")]
    public string Source { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "rank_position")]
    public int RankPosition { get; set; }

    [SugarColumn(ColumnName = "capability_score")]
    public decimal CapabilityScore { get; set; }

    [SugarColumn(ColumnName = "snapshot_at")]
    public DateTime SnapshotAt { get; set; }
}
