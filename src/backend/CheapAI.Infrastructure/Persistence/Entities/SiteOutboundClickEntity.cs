using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("site_outbound_clicks")]
public sealed class SiteOutboundClickEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "site_id")]
    public ulong SiteId { get; set; }

    [SugarColumn(ColumnName = "site_slug")]
    public string SiteSlug { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "target_type")]
    public string TargetType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "target_url")]
    public string TargetUrl { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "source_path", IsNullable = true)]
    public string? SourcePath { get; set; }

    [SugarColumn(ColumnName = "referrer_host", IsNullable = true)]
    public string? ReferrerHost { get; set; }

    [SugarColumn(ColumnName = "ip_hash", IsNullable = true)]
    public string? IpHash { get; set; }

    [SugarColumn(ColumnName = "user_agent_hash", IsNullable = true)]
    public string? UserAgentHash { get; set; }

    [SugarColumn(ColumnName = "clicked_at")]
    public DateTime ClickedAt { get; set; }
}
