namespace CheapAI.Api.Common;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "CheapAi:BootstrapAdmin";

    public bool Enabled { get; init; } = true;

    public string Username { get; init; } = "admin";

    public string Password { get; init; } = "ChangeMe123!";

    public string DisplayName { get; init; } = "系统管理员";
}
