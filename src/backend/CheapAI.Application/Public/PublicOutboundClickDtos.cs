namespace CheapAI.Application.Public;

public sealed class PublicOutboundClickRequest
{
    public string TargetType { get; init; } = string.Empty;

    public string? SourcePath { get; init; }
}

public sealed class PublicOutboundClickContext
{
    public string? SourcePath { get; init; }

    public string? Referrer { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}

public interface IPublicOutboundClickRepository
{
    Task RecordAsync(
        string siteSlug,
        string targetType,
        PublicOutboundClickContext context,
        CancellationToken cancellationToken = default);
}
