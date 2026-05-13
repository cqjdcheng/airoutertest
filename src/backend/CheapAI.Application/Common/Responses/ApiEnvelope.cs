namespace CheapAI.Application.Common.Responses;

public sealed class ApiEnvelope<T>
{
    public int Code { get; init; }

    public string Message { get; init; } = "ok";

    public T? Data { get; init; }

    public string RequestId { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
