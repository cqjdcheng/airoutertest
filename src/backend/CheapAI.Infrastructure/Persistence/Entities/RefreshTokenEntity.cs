using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("refresh_tokens")]
public sealed class RefreshTokenEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "admin_user_id")]
    public ulong AdminUserId { get; set; }

    [SugarColumn(ColumnName = "token_hash")]
    public string TokenHash { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "user_agent", IsNullable = true)]
    public string? UserAgent { get; set; }

    [SugarColumn(ColumnName = "ip_address", IsNullable = true)]
    public string? IpAddress { get; set; }

    [SugarColumn(ColumnName = "expires_at")]
    public DateTime ExpiresAt { get; set; }

    [SugarColumn(ColumnName = "revoked_at", IsNullable = true)]
    public DateTime? RevokedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
