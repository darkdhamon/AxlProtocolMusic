using AxlProtocolMusic.WebApp.Services.ServiceModels;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IImageStorageService
{
    Task<ImageSaveResult> SaveReleaseImageAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);

    bool IsManagedImageUrl(string? imageUrl);

    Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
}
