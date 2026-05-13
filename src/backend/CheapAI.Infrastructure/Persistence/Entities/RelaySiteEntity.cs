using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("relay_sites")]
public sealed class RelaySiteEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "slug")]
    public string Slug { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "base_url")]
    public string BaseUrl { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "website_url", IsNullable = true)]
    public string? WebsiteUrl { get; set; }

    [SugarColumn(ColumnName = "description", IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "supports_refund")]
    public bool SupportsRefund { get; set; }

    [SugarColumn(ColumnName = "supports_invoice")]
    public bool SupportsInvoice { get; set; }

    [SugarColumn(ColumnName = "has_docs")]
    public bool HasDocs { get; set; }

    [SugarColumn(ColumnName = "docs_url", IsNullable = true)]
    public string? DocsUrl { get; set; }

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "draft";

    [SugarColumn(ColumnName = "invite_url", IsNullable = true)]
    public string? InviteUrl { get; set; }

    [SugarColumn(ColumnName = "recent_review", IsNullable = true)]
    public string? RecentReview { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }

    [SugarColumn(ColumnName = "deleted_at", IsNullable = true)]
    public DateTime? DeletedAt { get; set; }

    [SugarColumn(ColumnName = "created_by", IsNullable = true)]
    public ulong? CreatedBy { get; set; }

    [SugarColumn(ColumnName = "updated_by", IsNullable = true)]
    public ulong? UpdatedBy { get; set; }
}
