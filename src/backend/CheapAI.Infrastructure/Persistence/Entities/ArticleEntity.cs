using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("articles")]
public sealed class ArticleEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "category_id", IsNullable = true)]
    public ulong? CategoryId { get; set; }

    [SugarColumn(ColumnName = "slug")]
    public string Slug { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "title")]
    public string Title { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "summary", IsNullable = true)]
    public string? Summary { get; set; }

    [SugarColumn(ColumnName = "content_md")]
    public string ContentMd { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "draft";

    [SugarColumn(ColumnName = "published_at", IsNullable = true)]
    public DateTime? PublishedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }
}
