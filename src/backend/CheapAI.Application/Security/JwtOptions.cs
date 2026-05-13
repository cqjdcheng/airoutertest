namespace CheapAI.Application.Security;

public sealed class JwtOptions
{
    public const string SectionName = "CheapAi:Jwt";

    public string Issuer { get; init; } = "cheapai-api";

    public string Audience { get; init; } = "cheapai-admin";

    public string SigningKey { get; init; } = "change-me-to-a-long-random-secret";

    public int AccessTokenMinutes { get; init; } = 60;

    public int RefreshTokenDays { get; init; } = 14;
}
