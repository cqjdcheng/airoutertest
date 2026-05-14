namespace CheapAI.Infrastructure.Participation;

public sealed class TurnstileOptions
{
    public const string SectionName = "CheapAi:Turnstile";

    public string SecretKey { get; init; } = string.Empty;

    public string SiteVerifyUrl { get; init; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}
