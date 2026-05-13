namespace CheapAI.Domain.Common;

public abstract class AuditableEntity : EntityBase
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAtUtc { get; set; }

    public ulong? CreatedBy { get; set; }

    public ulong? UpdatedBy { get; set; }
}
