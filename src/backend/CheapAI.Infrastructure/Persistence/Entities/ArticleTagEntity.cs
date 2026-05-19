using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("article_tags")]
public sealed class ArticleTagEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "slug")]
    public string Slug { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; } = 1000;

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }

    [SugarColumn(ColumnName = "deleted_at", IsNullable = true)]
    public DateTime? DeletedAt { get; set; }
}
