using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;

namespace AxlProtocolMusic.WebApp.Services.Identity;

public sealed class ReleaseSeedService
{
    private readonly IRepository<Release> _releaseRepository;

    public ReleaseSeedService(IRepository<Release> releaseRepository)
    {
        _releaseRepository = releaseRepository;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingReleases = await _releaseRepository.GetAllAsync(cancellationToken);
        if (existingReleases.Count > 0)
        {
            return;
        }

        foreach (var release in GetSeedReleases())
        {
            await _releaseRepository.CreateAsync(release, cancellationToken);
        }
    }

    private static IReadOnlyList<Release> GetSeedReleases()
    {
        return
        [
            new Release
            {
                Id = "release-birthday-reboot-2026",
                Title = "Birthday Reboot",
                Slug = "birthday-reboot",
                ShortDescription = "Single released by Axl Protocol Music on February 22, 2026.",
                CoverImageUrl = string.Empty,
                ReleaseDateUtc = new DateTimeOffset(2026, 2, 22, 0, 0, 0, TimeSpan.Zero),
                IsPublished = true
            },
            new Release
            {
                Id = "release-birthday-lights-up-2026",
                Title = "Birthday, Lights Up",
                Slug = "birthday-lights-up",
                ShortDescription = "Single released by Axl Protocol Music on February 22, 2026.",
                CoverImageUrl = string.Empty,
                ReleaseDateUtc = new DateTimeOffset(2026, 2, 22, 0, 0, 0, TimeSpan.Zero),
                IsPublished = true
            },
            new Release
            {
                Id = "release-january-reckoning-2026",
                Title = "January Reckoning",
                Slug = "january-reckoning",
                ShortDescription = "Single released by Axl Protocol Music on January 26, 2026.",
                CoverImageUrl = string.Empty,
                ReleaseDateUtc = new DateTimeOffset(2026, 1, 26, 0, 0, 0, TimeSpan.Zero),
                IsPublished = true
            },
            new Release
            {
                Id = "release-raise-the-hammer-light-the-hall-2026",
                Title = "Raise The Hammer Light the Hall",
                Slug = "raise-the-hammer-light-the-hall",
                ShortDescription = "Single released by Axl Protocol Music on January 20, 2026.",
                CoverImageUrl = string.Empty,
                ReleaseDateUtc = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero),
                IsPublished = true
            }
        ];
    }
}
