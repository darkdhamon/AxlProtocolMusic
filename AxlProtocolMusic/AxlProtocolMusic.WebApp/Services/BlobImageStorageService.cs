using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class BlobImageStorageService : IImageStorageService
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly ImageStorageSettings _settings;

    public BlobImageStorageService(IOptions<ImageStorageSettings> settings)
    {
        _settings = settings.Value;

        if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
        {
            throw new InvalidOperationException("ImageStorage:ConnectionString must be configured for blob storage.");
        }

        if (string.IsNullOrWhiteSpace(_settings.ContainerName))
        {
            throw new InvalidOperationException("ImageStorage:ContainerName must be configured for blob storage.");
        }

        var blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
        _blobContainerClient = blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
    }

    public async Task<ImageSaveResult> SaveReleaseImageAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        DiskImageStorageService.ValidateImage(file, _settings.MaxFileSizeBytes);

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

        var blobName = $"releases/{Guid.NewGuid():N}{extension}";
        await _blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        await using var readStream = file.OpenReadStream();
        await blobClient.UploadAsync(
            readStream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                }
            },
            cancellationToken);

        return new ImageSaveResult
        {
            Url = blobClient.Uri.AbsoluteUri,
            StoragePath = blobName
        };
    }

    public bool IsManagedImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri))
        {
            return false;
        }

        var containerUri = _blobContainerClient.Uri;
        if (!string.Equals(imageUri.Host, containerUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var expectedPrefix = $"{containerUri.AbsolutePath.TrimEnd('/')}/";
        return imageUri.AbsolutePath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var blobName = GetBlobName(storagePath);
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return Task.CompletedTask;
        }

        return _blobContainerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
    }

    private string GetBlobName(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(storagePath, UriKind.Absolute, out var storageUri))
        {
            var containerPath = _blobContainerClient.Uri.AbsolutePath.TrimEnd('/');
            var storagePathValue = storageUri.AbsolutePath;

            if (storagePathValue.StartsWith($"{containerPath}/", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(storagePathValue[(containerPath.Length + 1)..]);
            }
        }

        return storagePath.TrimStart('/');
    }
}
