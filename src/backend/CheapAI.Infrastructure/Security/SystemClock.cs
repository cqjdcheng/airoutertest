using CheapAI.Application.Common.Abstractions;

namespace CheapAI.Infrastructure.Security;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
