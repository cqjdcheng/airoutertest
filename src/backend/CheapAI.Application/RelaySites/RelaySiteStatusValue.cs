namespace CheapAI.Application.RelaySites;

public static class RelaySiteStatusValue
{
    public const string Draft = "draft";
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Archived = "archived";

    public static readonly HashSet<string> All =
    [
        Draft,
        Active,
        Suspended,
        Archived
    ];
}
