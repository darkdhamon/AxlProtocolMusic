using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Interfaces;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class ReleaseService : IReleaseService
{
    private readonly IRepository<Release> _releaseRepository;

    public ReleaseService(IRepository<Release> releaseRepository)
    {
        _releaseRepository = releaseRepository;
    }

    public async Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var lastMonthThreshold = now.AddMonths(-1);

        var publishedReleases = (await _releaseRepository.GetAllAsync(cancellationToken))
            .Where(release => release.IsPublished)
            .OrderByDescending(release => release.ReleaseDateUtc)
            .ToList();

        var releasesFromLastMonth = publishedReleases
            .Where(release => release.ReleaseDateUtc >= lastMonthThreshold)
            .ToList();

        var featuredReleases = releasesFromLastMonth.Count >= 3
            ? releasesFromLastMonth
            : publishedReleases.Take(3).ToList();

        return featuredReleases
            .Select(release => new FeaturedReleaseViewModel
            {
                Title = release.Title,
                Slug = release.Slug,
                ShortDescription = release.ShortDescription,
                CoverImageUrl = release.CoverImageUrl,
                ReleaseDateUtc = release.ReleaseDateUtc
            })
            .ToList();
    }
}
