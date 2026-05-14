using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("model_providers")]
public sealed class ModelProviderEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "slug")]
    public string Slug { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "website_url", IsNullable = true)]
    public string? WebsiteUrl { get; set; }

    [SugarColumn(ColumnName = "description", IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "active";

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; } = 1000;

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }

    [SugarColumn(ColumnName = "deleted_at", IsNullable = true)]
    public DateTime? DeletedAt { get; set; }
}
