using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
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
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return false;
        }

        return imageUrl.StartsWith($"/{_settings.UploadRoot}/", StringComparison.OrdinalIgnoreCase)
            || imageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return Task.CompletedTask;
        }

        var physicalPath = TryResolveManagedPhysicalPath(storagePath);
        if (physicalPath is null)
        {
            return Task.CompletedTask;
        }

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }

    private string? TryResolveManagedPhysicalPath(string storagePath)
    {
        var normalizedStoragePath = storagePath.Trim();
        if (string.IsNullOrWhiteSpace(normalizedStoragePath))
        {
            return null;
        }

        var relativePath = normalizedStoragePath
            .TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        string candidatePath;
        try
        {
            candidatePath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, relativePath));
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (PathTooLongException)
        {
            return null;
        }

        foreach (var uploadRootPath in GetAllowedUploadRootPaths())
        {
            if (IsPathWithinRoot(candidatePath, uploadRootPath))
            {
                return candidatePath;
            }
        }

        return null;
    }

    private IEnumerable<string> GetAllowedUploadRootPaths()
    {
        var uploadRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizeUploadRoot(_settings.UploadRoot),
            "uploads"
        };

        foreach (var uploadRoot in uploadRoots)
        {
            yield return Path.GetFullPath(Path.Combine(_environment.WebRootPath, uploadRoot));
        }
    }

    private static string NormalizeUploadRoot(string uploadRoot)
    {
        return uploadRoot
            .Trim()
            .Trim('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
    }

    private static bool IsPathWithinRoot(string candidatePath, string rootPath)
    {
        if (string.Equals(candidatePath, rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var rootWithSeparator = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;

        return candidatePath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
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
