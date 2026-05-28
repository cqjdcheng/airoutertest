using System.Security.Cryptography;
using System.Text;
using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.Public;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class PublicOutboundClickRepository(ISqlSugarClient db) : IPublicOutboundClickRepository
{
    public async Task RecordAsync(
        string siteSlug,
        string targetType,
        PublicOutboundClickContext context,
        CancellationToken cancellationToken = default)
    {
        var site = await db.Queryable<RelaySiteEntity>()
            .Where(x => x.Slug == siteSlug && x.DeletedAt == null && x.Status == "active")
            .Select(x => new OutboundSiteRow
            {
                Id = x.Id,
                Slug = x.Slug,
                WebsiteUrl = x.WebsiteUrl,
                InviteUrl = x.InviteUrl,
                DocsUrl = x.DocsUrl
            })
            .FirstAsync(cancellationToken);

        if (site is null)
        {
            throw new AppNotFoundException("站点不存在");
        }

        var targetUrl = ResolveTargetUrl(site, targetType);
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            throw new AppNotFoundException("跳转目标不存在");
        }

        await db.Insertable(new SiteOutboundClickEntity
        {
            SiteId = site.Id,
            SiteSlug = site.Slug,
            TargetType = targetType,
            TargetUrl = Truncate(targetUrl, 512),
            SourcePath = NormalizePath(context.SourcePath),
            ReferrerHost = NormalizeReferrerHost(context.Referrer),
            IpHash = HashOptional(context.IpAddress),
            UserAgentHash = HashOptional(context.UserAgent),
            ClickedAt = DateTime.UtcNow
        }).ExecuteCommandAsync(cancellationToken);
    }

    private static string? ResolveTargetUrl(OutboundSiteRow site, string targetType)
    {
        return targetType switch
        {
            "website" => site.WebsiteUrl,
            "invite" => site.InviteUrl,
            "docs" => site.DocsUrl,
            _ => null
        };
    }

    private static string? NormalizePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.StartsWith("/", StringComparison.Ordinal)
            ? Truncate(normalized, 255)
            : null;
    }

    private static string? NormalizeReferrerHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return null;
        }

        return Truncate(uri.Host, 128);
    }

    private static string? HashOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private sealed class OutboundSiteRow
    {
        public ulong Id { get; init; }

        public string Slug { get; init; } = string.Empty;

        public string? WebsiteUrl { get; init; }

        public string? InviteUrl { get; init; }

        public string? DocsUrl { get; init; }
    }
}
