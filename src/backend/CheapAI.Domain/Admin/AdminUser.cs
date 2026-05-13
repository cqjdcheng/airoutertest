using CheapAI.Domain.Common;

namespace CheapAI.Domain.Admin;

public sealed class AdminUser : AuditableEntity
{
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public AdminUserStatus Status { get; set; } = AdminUserStatus.Active;

    public DateTime? LastLoginAtUtc { get; set; }
}
