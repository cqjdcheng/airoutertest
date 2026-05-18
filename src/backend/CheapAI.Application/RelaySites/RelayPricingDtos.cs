namespace CheapAI.Application.RelaySites;

public sealed class RelayPricingPreviewRequest
{
    public string BaseUrl { get; init; } = string.Empty;

    public string ProviderType { get; init; } = RelayPricingProviderType.Auto;

    public string? ApiKey { get; init; }

    public decimal RechargeRatio { get; init; } = 1;

    public decimal BonusRatio { get; init; }

    public decimal RateBaseline { get; init; } = 0.002m;

    public string? GroupId { get; init; }
}

public sealed class RelayPricingPreviewItemResponse
{
    public string OfficialModelId { get; init; } = string.Empty;

    public string RequestName { get; init; } = string.Empty;

    public string ApiType { get; init; } = "openai";

    public string DisplayName { get; init; } = string.Empty;

    public string BillingType { get; init; } = RelayPricingBillingType.Tokens;

    public string GroupId { get; init; } = string.Empty;

    public string GroupName { get; init; } = string.Empty;

    public decimal GroupRate { get; init; } = 1;

    public decimal ModelRate { get; init; } = 1;

    public decimal CompletionRatio { get; init; } = 1;

    public decimal? SiteInputPriceUsd { get; init; }

    public decimal? SiteOutputPriceUsd { get; init; }

    public decimal? SitePerCallPriceUsd { get; init; }

    public decimal? EffectiveInputPriceUsd { get; init; }

    public decimal? EffectiveOutputPriceUsd { get; init; }

    public decimal? EffectivePerCallPriceUsd { get; init; }

    public decimal RechargeRatio { get; init; } = 1;

    public decimal BonusRatio { get; init; }

    public string SourceType { get; init; } = "crawl";

    public string Status { get; init; } = "active";
}

public static class RelayPricingProviderType
{
    public const string Auto = "auto";
    public const string NewApi = "newapi";
    public const string OneApi = "oneapi";
    public const string OneHub = "onehub";

    public static readonly HashSet<string> All = [Auto, NewApi, OneApi, OneHub];
}

public static class RelayPricingBillingType
{
    public const string Tokens = "tokens";
    public const string Times = "times";
}
