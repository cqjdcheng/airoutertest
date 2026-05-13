using CheapAI.Application.Common.Abstractions;

namespace CheapAI.BackgroundJobs;

public sealed class BackgroundAdminAccessor : ICurrentAdminAccessor
{
    public ulong? AdminUserId => null;
}
