namespace CheapAI.Application.Models;

public sealed class ModelImportPreviewRequest
{
    public string BaseUrl { get; init; } = string.Empty;

    public string? ApiKey { get; init; }

    public string Vendor { get; init; } = string.Empty;
}

public sealed class ModelImportPreviewItemResponse
{
    public string ProviderSlug { get; init; } = string.Empty;

    public string ProviderName { get; init; } = string.Empty;

    public string Vendor { get; init; } = string.Empty;

    public string OfficialModelId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal? OfficialInputPriceUsd { get; init; }

    public decimal? OfficialOutputPriceUsd { get; init; }

    public decimal? CapabilityScore { get; init; }

    public string? CapabilitySource { get; init; }
}

public sealed class ImportModelsRequest
{
    public IReadOnlyList<ImportModelItemRequest> Models { get; init; } = [];
}

public sealed class ImportModelItemRequest
{
    public ulong? ProviderId { get; init; }

    public string? ProviderSlug { get; init; }

    public string? ProviderName { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Vendor { get; init; } = string.Empty;

    public string OfficialModelId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string Status { get; init; } = ModelStatusValue.Active;

    public bool IsHot { get; init; }

    public int SortOrder { get; init; } = 1000;

    public decimal? OfficialInputPriceUsd { get; init; }

    public decimal? OfficialOutputPriceUsd { get; init; }

    public decimal? CapabilityScore { get; init; }

    public string? CapabilitySource { get; init; }
}

public sealed class ImportModelsResponse
{
    public int CreatedCount { get; init; }

    public int UpdatedCount { get; init; }
}
