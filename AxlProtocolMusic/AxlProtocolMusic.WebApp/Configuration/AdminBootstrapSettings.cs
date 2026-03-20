namespace AxlProtocolMusic.WebApp.Configuration;

public sealed class AdminBootstrapSettings
{
    public const string SectionName = "AdminBootstrap";

    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string RoleName { get; init; } = "Admin";
}
