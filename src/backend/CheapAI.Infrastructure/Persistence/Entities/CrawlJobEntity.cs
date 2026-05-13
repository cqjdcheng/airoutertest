using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("crawl_jobs")]
public sealed class CrawlJobEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "site_id", IsNullable = true)]
    public ulong? SiteId { get; set; }

    [SugarColumn(ColumnName = "job_type")]
    public string JobType { get; set; } = "manual_price_crawl";

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "pending";

    [SugarColumn(ColumnName = "priority")]
    public int Priority { get; set; } = 5;

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
