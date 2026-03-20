namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IReleaseService
{
    Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(
        CancellationToken cancellationToken = default);
}
