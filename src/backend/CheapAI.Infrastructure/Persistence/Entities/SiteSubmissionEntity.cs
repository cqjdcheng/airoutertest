using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("site_submissions")]
public sealed class SiteSubmissionEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "site_name")]
    public string SiteName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "site_url")]
    public string SiteUrl { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "contact", IsNullable = true)]
    public string? Contact { get; set; }

    [SugarColumn(ColumnName = "description", IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "review_status")]
    public string ReviewStatus { get; set; } = "pending";

    [SugarColumn(ColumnName = "review_note", IsNullable = true)]
    public string? ReviewNote { get; set; }

    [SugarColumn(ColumnName = "reviewed_at", IsNullable = true)]
    public DateTime? ReviewedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }
}
