namespace CheapAI.Application.Auth;

public sealed class AdminMeResponse
{
    public ulong Id { get; init; }

    public string Username { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;
}
