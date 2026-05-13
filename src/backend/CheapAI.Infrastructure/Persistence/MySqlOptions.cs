namespace CheapAI.Infrastructure.Persistence;

public sealed class MySqlOptions
{
    public const string SectionName = "CheapAi:MySql";

    public string ConnectionString { get; init; } = string.Empty;
}
