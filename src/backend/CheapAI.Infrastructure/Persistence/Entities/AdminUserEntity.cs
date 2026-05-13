using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Entities;

[SugarTable("admin_users")]
public sealed class AdminUserEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public ulong Id { get; set; }

    [SugarColumn(ColumnName = "username")]
    public string Username { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "email", IsNullable = true)]
    public string? Email { get; set; }

    [SugarColumn(ColumnName = "phone", IsNullable = true)]
    public string? Phone { get; set; }

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "active";

    [SugarColumn(ColumnName = "last_login_at", IsNullable = true)]
    public DateTime? LastLoginAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }
}
