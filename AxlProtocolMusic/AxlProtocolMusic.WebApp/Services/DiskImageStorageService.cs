using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class DiskImageStorageService : IImageStorageService
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    ];

    private readonly IWebHostEnvironment _environment;
    private readonly ImageStorageSettings _settings;

    public DiskImageStorageService(
        IWebHostEnvironment environment,
        IOptions<ImageStorageSettings> settings)
    {
        _environment = environment;
        _settings = settings.Value;
    }

    public async Task<ImageSaveResult> SaveReleaseImageAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateImage(file, _settings.MaxFileSizeBytes);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = file.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => string.Empty
            };
        }

        var relativeDirectory = Path.Combine(_settings.UploadRoot, "releases");
        var physicalDirectory = Path.Combine(_environment.WebRootPath, relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(physicalDirectory, fileName);

        await using var fileStream = new FileStream(physicalPath, FileMode.Create);
        await file.CopyToAsync(fileStream, cancellationToken);

        var relativePath = Path.Combine(relativeDirectory, fileName).Replace("\\", "/");

        return new ImageSaveResult
        {
            Url = $"/{relativePath}",
            StoragePath = relativePath
        };
    }

    public bool IsManagedImageUrl(string? imageUrl)
    {
        return !string.IsNullOrWhiteSpace(imageUrl)
            && imageUrl.StartsWith($"/{_settings.UploadRoot}/", StringComparison.OrdinalIgnoreCase);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return Task.CompletedTask;
        }

        var normalizedPath = storagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        var physicalPath = Path.Combine(_environment.WebRootPath, normalizedPath);

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }

    internal static void ValidateImage(IFormFile file, long maxFileSizeBytes)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("The uploaded image file is empty.");
        }

        if (file.Length > maxFileSizeBytes)
        {
            throw new InvalidOperationException("The uploaded image exceeds the size limit.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException("Only JPG, PNG, WEBP, and GIF images are supported.");
        }
    }
}
