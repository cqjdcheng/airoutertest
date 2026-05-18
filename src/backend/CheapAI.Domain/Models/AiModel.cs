using CheapAI.Domain.Common;

namespace CheapAI.Domain.Models;

public sealed class AiModel : AuditableEntity
{
    public string Slug { get; set; } = string.Empty;

    public string Vendor { get; set; } = string.Empty;

    public string OfficialModelId { get; set; } = string.Empty;

    public string RequestName { get; set; } = string.Empty;

    public string ApiType { get; set; } = "openai";

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ModelStatus Status { get; set; } = ModelStatus.Active;
}
