using CheapAI.Application.Common.Exceptions;

namespace CheapAI.Application.Public;

public sealed class PublicOutboundClickService(IPublicOutboundClickRepository repository)
{
    private static readonly HashSet<string> AllowedTargetTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "website",
        "invite",
        "docs"
    };

    public static string? NormalizeTargetType(string? targetType)
    {
        if (string.IsNullOrWhiteSpace(targetType))
        {
            return null;
        }

        var normalized = targetType.Trim().ToLowerInvariant();
        return AllowedTargetTypes.Contains(normalized) ? normalized : null;
    }

    public async Task RecordAsync(
        string siteSlug,
        string targetType,
        PublicOutboundClickContext context,
        CancellationToken cancellationToken = default)
    {
        var normalizedTargetType = NormalizeTargetType(targetType);
        if (normalizedTargetType is null)
        {
            throw new AppNotFoundException("跳转目标不存在");
        }

        await repository.RecordAsync(siteSlug, normalizedTargetType, context, cancellationToken);
    }
}
