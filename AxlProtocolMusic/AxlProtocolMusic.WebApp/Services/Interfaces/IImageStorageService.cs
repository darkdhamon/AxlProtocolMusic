namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IImageStorageService
{
    Task<ImageSaveResult> SaveReleaseImageAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
}
