namespace AxlProtocolMusic.WebApp.Services;

public sealed class ReleaseCreateResult
{
    public bool Succeeded { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;
}
