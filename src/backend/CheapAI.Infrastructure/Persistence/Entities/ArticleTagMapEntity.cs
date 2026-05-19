using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("article_tag_maps")]
public sealed class ArticleTagMapEntity
{
    [SugarColumn(ColumnName = "article_id")]
    public ulong ArticleId { get; set; }

    [SugarColumn(ColumnName = "tag_id")]
    public ulong TagId { get; set; }
}
