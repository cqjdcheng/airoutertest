using System.Security.Claims;
using CheapAI.Application.Common.Abstractions;

namespace CheapAI.Api.Common;

public sealed class CurrentAdminAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentAdminAccessor
{
    public ulong? AdminUserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return ulong.TryParse(value, out var adminUserId) ? adminUserId : null;
        }
    }
}
