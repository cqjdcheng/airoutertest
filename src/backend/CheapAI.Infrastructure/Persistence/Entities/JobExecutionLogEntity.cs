using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("job_execution_logs")]
public sealed class JobExecutionLogEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "job_category")]
    public string JobCategory { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "job_id", IsNullable = true)]
    public ulong? JobId { get; set; }

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "succeeded";

    [SugarColumn(ColumnName = "message")]
    public string Message { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "started_at", IsNullable = true)]
    public DateTime? StartedAt { get; set; }

    [SugarColumn(ColumnName = "finished_at", IsNullable = true)]
    public DateTime? FinishedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
