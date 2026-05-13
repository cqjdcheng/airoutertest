namespace CheapAI.Application.Common.Abstractions;

public interface ICurrentAdminAccessor
{
    ulong? AdminUserId { get; }
}
