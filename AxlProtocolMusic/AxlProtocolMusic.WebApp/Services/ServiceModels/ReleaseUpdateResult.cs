namespace AxlProtocolMusic.WebApp.Services.ServiceModels;

public sealed class ReleaseUpdateResult
{
    public bool Succeeded { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public string ImageStoragePath { get; init; } = string.Empty;
}
