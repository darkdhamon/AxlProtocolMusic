namespace AxlProtocolMusic.WebApp.Configuration;

public sealed class ImageStorageSettings
{
    public const string SectionName = "ImageStorage";

    public string UploadRoot { get; init; } = "uploads";

    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;
}
