using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("site_settings")]
public sealed class SiteSettingEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public byte Id { get; set; }

    [SugarColumn(ColumnName = "site_name")]
    public string SiteName { get; set; } = "CheapAI";

    [SugarColumn(ColumnName = "site_icon_url", IsNullable = true)]
    public string? SiteIconUrl { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_by", IsNullable = true)]
    public ulong? UpdatedBy { get; set; }
}
