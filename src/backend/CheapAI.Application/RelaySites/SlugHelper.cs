using System.Text;
using System.Text.RegularExpressions;

namespace CheapAI.Application.RelaySites;

public static partial class SlugHelper
{
    public static string Normalize(string? explicitSlug, string fallback)
    {
        var source = string.IsNullOrWhiteSpace(explicitSlug) ? fallback : explicitSlug;
        var normalized = source.Trim().ToLowerInvariant();
        normalized = InvalidCharsRegex().Replace(normalized, "-");
        normalized = MultiDashRegex().Replace(normalized, "-").Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "item" : normalized;
    }

    [GeneratedRegex(@"[^a-z0-9\-]+", RegexOptions.Compiled)]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"\-+", RegexOptions.Compiled)]
    private static partial Regex MultiDashRegex();
}
