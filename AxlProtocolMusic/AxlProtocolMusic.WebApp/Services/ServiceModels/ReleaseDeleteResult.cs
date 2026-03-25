namespace AxlProtocolMusic.WebApp.Services;

public sealed class ReleaseDeleteResult
{
    public bool Succeeded { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public string ImageStoragePath { get; init; } = string.Empty;
}
