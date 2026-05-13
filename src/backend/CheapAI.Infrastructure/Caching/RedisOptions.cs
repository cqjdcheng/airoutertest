namespace CheapAI.Infrastructure.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "CheapAi:Redis";

    public string ConnectionString { get; init; } = string.Empty;
}
