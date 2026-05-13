using CheapAI.Domain.Common;

namespace CheapAI.Domain.Admin;

public sealed class RefreshToken : EntityBase
{
    public ulong AdminUserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public string? UserAgent { get; set; }

    public string? IpAddress { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
