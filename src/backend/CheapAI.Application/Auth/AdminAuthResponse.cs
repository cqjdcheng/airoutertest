namespace CheapAI.Application.Auth;

public sealed class AdminAuthResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public int ExpiresIn { get; init; }

    public AdminMeResponse Admin { get; init; } = new();

    public string RefreshToken { get; init; } = string.Empty;
}
