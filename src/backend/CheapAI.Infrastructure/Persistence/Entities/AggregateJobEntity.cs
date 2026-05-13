using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("aggregate_jobs")]
public sealed class AggregateJobEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "aggregate_type")]
    public string AggregateType { get; set; } = "ranking_snapshot";

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "pending";

    [SugarColumn(ColumnName = "requested_by", IsNullable = true)]
    public ulong? RequestedBy { get; set; }

    [SugarColumn(ColumnName = "started_at", IsNullable = true)]
    public DateTime? StartedAt { get; set; }

    [SugarColumn(ColumnName = "finished_at", IsNullable = true)]
    public DateTime? FinishedAt { get; set; }

    [SugarColumn(ColumnName = "error_message", IsNullable = true)]
    public string? ErrorMessage { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
