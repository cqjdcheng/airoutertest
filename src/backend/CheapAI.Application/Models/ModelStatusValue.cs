namespace CheapAI.Application.Models;

public static class ModelStatusValue
{
    public const string Active = "active";
    public const string Hidden = "hidden";
    public const string Deprecated = "deprecated";

    public static readonly HashSet<string> All =
    [
        Active,
        Hidden,
        Deprecated
    ];
}
