namespace CheapAI.Application.RelaySites;

public sealed class RelaySiteOfferUpsertRequest
{
    public ulong? ModelId { get; init; }

    public string? ModelSlug { get; init; }

    public string? Vendor { get; init; }

    public string? OfficialModelId { get; init; }

    public string? DisplayName { get; init; }

    public decimal? OfficialInputPriceUsd { get; init; }

    public decimal? OfficialOutputPriceUsd { get; init; }

    public decimal? SiteInputPriceUsd { get; init; }

    public decimal? SiteOutputPriceUsd { get; init; }

    public decimal RechargeRatio { get; init; } = 1;

    public decimal BonusRatio { get; init; }

    public string SourceType { get; init; } = "manual";

    public string Status { get; init; } = "active";
}

public sealed class RelaySiteOfferResponse
{
    public ulong Id { get; init; }

    public ulong ModelId { get; init; }

    public string ModelSlug { get; init; } = string.Empty;

    public string Vendor { get; init; } = string.Empty;

    public string OfficialModelId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public decimal? OfficialInputPriceUsd { get; init; }

    public decimal? OfficialOutputPriceUsd { get; init; }

    public decimal? SiteInputPriceUsd { get; init; }

    public decimal? SiteOutputPriceUsd { get; init; }

    public decimal? EffectiveInputPriceUsd { get; init; }

    public decimal? EffectiveOutputPriceUsd { get; init; }

    public decimal RechargeRatio { get; init; } = 1;

    public decimal BonusRatio { get; init; }

    public string SourceType { get; init; } = "manual";

    public string Status { get; init; } = "active";

    public DateTime? CrawledAt { get; init; }

    public DateTime? ReviewedAt { get; init; }
}

public sealed class RelaySiteTestRecordResponse
{
    public ulong Id { get; init; }

    public string ModelSlug { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string TestType { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int? FirstTokenMs { get; init; }

    public int? FullResponseMs { get; init; }

    public decimal RiskScore { get; init; }

    public string RiskLevel { get; init; } = "low";

    public string? ErrorMessage { get; init; }

    public DateTime TestedAt { get; init; }
}
