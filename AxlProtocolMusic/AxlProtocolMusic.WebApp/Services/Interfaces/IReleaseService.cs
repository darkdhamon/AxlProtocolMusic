using AxlProtocolMusic.WebApp.Models.Content;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IReleaseService
{
    Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(
        CancellationToken cancellationToken = default);

    Task<PagedReleaseResult> GetPagedReleasesAsync(
        string? searchTerm,
        int pageNumber,
        int pageSize,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default);

    Task<ReleaseDetailsViewModel?> GetReleaseBySlugAsync(
        string slug,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default);

    Task<ReleaseUpdateResult> UpdateReleaseAsync(
        ReleaseUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ReleaseCreateResult> CreateReleaseAsync(
        ReleaseUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ReleaseDeleteResult> DeleteReleaseAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<string> GenerateUniqueSlugAsync(
        string? value,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetKnownCreditRolesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetKnownContributorNamesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetKnownTagsAsync(
        CancellationToken cancellationToken = default);

    bool IsManagedImageUrl(string? imageUrl);
}
