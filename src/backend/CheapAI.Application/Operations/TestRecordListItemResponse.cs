namespace CheapAI.Application.Operations;

public sealed class TestRecordListItemResponse
{
    public ulong Id { get; init; }

    public string SiteName { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string TestType { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int? FirstTokenMs { get; init; }

    public int? FullResponseMs { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTime TestedAt { get; init; }
}
