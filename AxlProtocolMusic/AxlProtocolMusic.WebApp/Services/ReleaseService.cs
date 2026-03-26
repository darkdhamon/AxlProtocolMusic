using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using System.Text;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class ReleaseService : IReleaseService
{
    private const int MinimumPageSize = 1;
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

        var publishedReleases = await GetPublishedReleasesAsync(cancellationToken);

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

    public async Task<PagedReleaseResult> GetPagedReleasesAsync(
        string? searchTerm,
        int pageNumber,
        int pageSize,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearchTerm = searchTerm?.Trim() ?? string.Empty;
        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Max(MinimumPageSize, pageSize);

        var releases = includeUnpublished
            ? (await _releaseRepository.GetAllAsync(cancellationToken))
                .OrderBy(release => !string.IsNullOrWhiteSpace(release.CoverImageUrl))
                .ThenByDescending(release => release.ReleaseDateUtc)
                .ToList()
            : await GetPublishedReleasesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            releases = releases
                .Where(release =>
                    release.Title.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase)
                    || release.ShortDescription.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase)
                    || release.Slug.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var totalCount = releases.Count;
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)safePageSize);

        if (safePageNumber > totalPages)
        {
            safePageNumber = totalPages;
        }

        var items = releases
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .Select(release => new ReleaseListItemViewModel
            {
                Title = release.Title,
                Slug = release.Slug,
                ShortDescription = release.ShortDescription,
                CoverImageUrl = release.CoverImageUrl,
                ReleaseDateUtc = release.ReleaseDateUtc,
                IsPublished = release.IsPublished
            })
            .ToList();

        return new PagedReleaseResult
        {
            Items = items,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            SearchTerm = normalizedSearchTerm
        };
    }

    public async Task<ReleaseDetailsViewModel?> GetReleaseBySlugAsync(
        string slug,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default)
    {
        var releases = includeUnpublished
            ? (await _releaseRepository.GetAllAsync(cancellationToken))
                .OrderByDescending(release => release.ReleaseDateUtc)
                .ToList()
            : await GetPublishedReleasesAsync(cancellationToken);

        var release = releases
            .FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));

        if (release is null)
        {
            return null;
        }

        if (!release.Tracks.Any()
            && !string.IsNullOrWhiteSpace(release.Lyrics))
        {
            release.Tracks =
            [
                new ReleaseTrack
                {
                    Title = release.Title,
                    Lyrics = release.Lyrics.Trim()
                }
            ];
            release.Lyrics = string.Empty;
            await _releaseRepository.UpdateAsync(release, cancellationToken);
        }

        return new ReleaseDetailsViewModel
        {
            Id = release.Id,
            Title = release.Title,
            Slug = release.Slug,
            ShortDescription = release.ShortDescription,
            CoverImageUrl = release.CoverImageUrl,
            Story = release.Story,
            Credits = release.Credits
                .Select(credit => new ReleaseCredit
                {
                    Name = credit.Name,
                    Roles = credit.Roles.ToList()
                })
                .ToList(),
            Tracks = release.Tracks
                .Select(track => new ReleaseTrack
                {
                    Title = track.Title,
                    Duration = track.Duration,
                    Lyrics = track.Lyrics
                })
                .ToList(),
            Links = release.Links
                .Select(link => new ReleaseLink
                {
                    PlatformName = link.PlatformName,
                    Url = link.Url
                })
                .ToList(),
            ReleaseType = GetDisplayReleaseType(release),
            ReleaseTypeOverride = release.ReleaseTypeOverride,
            Tags = release.Tags.ToList(),
            ReleaseDateUtc = release.ReleaseDateUtc,
            IsPublished = release.IsPublished
        };
    }

    public async Task<ReleaseUpdateResult> UpdateReleaseAsync(
        ReleaseUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var releases = await _releaseRepository.GetAllAsync(cancellationToken);

        var release = releases.FirstOrDefault(item =>
            string.Equals(item.Slug, request.OriginalSlug, StringComparison.OrdinalIgnoreCase));

        if (release is null)
        {
            return new ReleaseUpdateResult
            {
                Succeeded = false,
                ErrorMessage = "The release could not be found."
            };
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return new ReleaseUpdateResult
            {
                Succeeded = false,
                ErrorMessage = "Slug is required."
            };
        }

        var slugInUse = releases.Any(item =>
            !string.Equals(item.Id, release.Id, StringComparison.Ordinal)
            && string.Equals(item.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));

        if (slugInUse)
        {
            return new ReleaseUpdateResult
            {
                Succeeded = false,
                ErrorMessage = "That release slug is already in use."
            };
        }

        release.Title = request.Title.Trim();
        release.Slug = normalizedSlug;
        release.ShortDescription = request.ShortDescription.Trim();
        release.CoverImageUrl = request.CoverImageUrl?.Trim() ?? string.Empty;
        release.Story = request.Story?.Trim() ?? string.Empty;
        release.Lyrics = string.Empty;
        release.Credits = NormalizeCredits(request.Credits);
        release.Tracks = NormalizeTracks(request.Tracks);
        release.Links = NormalizeLinks(request.Links);
        release.ReleaseTypeOverride = NormalizeReleaseTypeOverride(request.ReleaseTypeOverride);
        release.Tags = NormalizeTags(request.Tags);
        release.ReleaseDateUtc = new DateTimeOffset(
            DateTime.SpecifyKind(request.ReleaseDate.Date, DateTimeKind.Utc));
        release.IsPublished = request.IsPublished;

        await _releaseRepository.UpdateAsync(release, cancellationToken);

        return new ReleaseUpdateResult
        {
            Succeeded = true,
            Slug = release.Slug,
            ImageStoragePath = release.CoverImageUrl
        };
    }

    public async Task<ReleaseCreateResult> CreateReleaseAsync(
        ReleaseUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var releases = await _releaseRepository.GetAllAsync(cancellationToken);

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return new ReleaseCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Slug is required."
            };
        }

        var slugInUse = releases.Any(item =>
            string.Equals(item.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));

        if (slugInUse)
        {
            return new ReleaseCreateResult
            {
                Succeeded = false,
                ErrorMessage = "That release slug is already in use."
            };
        }

        var release = new Release
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = request.Title.Trim(),
            Slug = normalizedSlug,
            ShortDescription = request.ShortDescription.Trim(),
            CoverImageUrl = request.CoverImageUrl?.Trim() ?? string.Empty,
            Story = request.Story?.Trim() ?? string.Empty,
            Lyrics = string.Empty,
            Credits = NormalizeCredits(request.Credits),
            Tracks = NormalizeTracks(request.Tracks),
            Links = NormalizeLinks(request.Links),
            ReleaseTypeOverride = NormalizeReleaseTypeOverride(request.ReleaseTypeOverride),
            Tags = NormalizeTags(request.Tags),
            ReleaseDateUtc = new DateTimeOffset(
                DateTime.SpecifyKind(request.ReleaseDate.Date, DateTimeKind.Utc)),
            IsPublished = request.IsPublished
        };

        await _releaseRepository.CreateAsync(release, cancellationToken);

        return new ReleaseCreateResult
        {
            Succeeded = true,
            Slug = release.Slug
        };
    }

    public async Task<ReleaseDeleteResult> DeleteReleaseAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return new ReleaseDeleteResult
            {
                Succeeded = false,
                ErrorMessage = "The release slug is required."
            };
        }

        var releases = await _releaseRepository.GetAllAsync(cancellationToken);
        var release = releases.FirstOrDefault(item =>
            string.Equals(item.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));

        if (release is null)
        {
            return new ReleaseDeleteResult
            {
                Succeeded = false,
                ErrorMessage = "The release could not be found."
            };
        }

        await _releaseRepository.DeleteAsync(release.Id, cancellationToken);

        return new ReleaseDeleteResult
        {
            Succeeded = true,
            ImageStoragePath = release.CoverImageUrl
        };
    }

    public async Task<string> GenerateUniqueSlugAsync(
        string? value,
        CancellationToken cancellationToken = default)
    {
        var baseSlug = NormalizeSlug(value);
        var releases = await _releaseRepository.GetAllAsync(cancellationToken);

        if (!releases.Any(item => string.Equals(item.Slug, baseSlug, StringComparison.OrdinalIgnoreCase)))
        {
            return baseSlug;
        }

        var datedSlug = $"{baseSlug}-{DateTimeOffset.UtcNow:yyMMdd}";
        if (!releases.Any(item => string.Equals(item.Slug, datedSlug, StringComparison.OrdinalIgnoreCase)))
        {
            return datedSlug;
        }

        var suffix = 2;
        while (releases.Any(item => string.Equals(item.Slug, $"{datedSlug}-{suffix}", StringComparison.OrdinalIgnoreCase)))
        {
            suffix++;
        }

        return $"{datedSlug}-{suffix}";
    }

    public async Task<IReadOnlyList<string>> GetKnownCreditRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return (await _releaseRepository.GetAllAsync(cancellationToken))
            .SelectMany(release => release.Credits)
            .SelectMany(credit => credit.Roles)
            .Select(role => role.Trim())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(role => role)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetKnownContributorNamesAsync(
        CancellationToken cancellationToken = default)
    {
        return (await _releaseRepository.GetAllAsync(cancellationToken))
            .SelectMany(release => release.Credits)
            .Select(credit => credit.Name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.First())
            .ToList();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetKnownContributorRolesByNameAsync(
        CancellationToken cancellationToken = default)
    {
        return (await _releaseRepository.GetAllAsync(cancellationToken))
            .SelectMany(release => release.Credits)
            .Where(credit => !string.IsNullOrWhiteSpace(credit.Name))
            .GroupBy(credit => credit.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .SelectMany(credit => credit.Roles)
                    .Select(role => role.Trim())
                    .Where(role => !string.IsNullOrWhiteSpace(role))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(role => role)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<string>> GetKnownTagsAsync(
        CancellationToken cancellationToken = default)
    {
        return (await _releaseRepository.GetAllAsync(cancellationToken))
            .SelectMany(release => release.Tags)
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.First())
            .ToList();
    }

    public bool IsManagedImageUrl(string? imageUrl)
    {
        return !string.IsNullOrWhiteSpace(imageUrl)
            && imageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<Release>> GetPublishedReleasesAsync(CancellationToken cancellationToken)
    {
        return (await _releaseRepository.GetAllAsync(cancellationToken))
            .Where(release => release.IsPublished)
            .OrderByDescending(release => release.ReleaseDateUtc)
            .ToList();
    }

    private static string NormalizeSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "release";
        }

        var slugBuilder = new StringBuilder();
        var previousWasHyphen = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                slugBuilder.Append(character);
                previousWasHyphen = false;
                continue;
            }

            if (character is ' ' or '-' or '_')
            {
                if (!previousWasHyphen && slugBuilder.Length > 0)
                {
                    slugBuilder.Append('-');
                    previousWasHyphen = true;
                }
            }
        }

        var slug = slugBuilder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "release" : slug;
    }

    private static List<ReleaseCredit> NormalizeCredits(IEnumerable<ReleaseCredit>? credits)
    {
        if (credits is null)
        {
            return [];
        }

        return credits
            .Select(credit => new ReleaseCredit
            {
                Name = credit.Name.Trim(),
                Roles = credit.Roles
                    .Select(role => role.Trim())
                    .Where(role => !string.IsNullOrWhiteSpace(role))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .Where(credit => !string.IsNullOrWhiteSpace(credit.Name) || credit.Roles.Count > 0)
            .ToList();
    }

    private static List<string> NormalizeTags(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            return [];
        }

        return tags
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ReleaseTrack> NormalizeTracks(IEnumerable<ReleaseTrack>? tracks)
    {
        if (tracks is null)
        {
            return [];
        }

        return tracks
            .Select(track => new ReleaseTrack
            {
                Title = track.Title.Trim(),
                Duration = track.Duration.Trim(),
                Lyrics = track.Lyrics.Trim()
            })
            .Where(track =>
                !string.IsNullOrWhiteSpace(track.Title)
                || !string.IsNullOrWhiteSpace(track.Duration)
                || !string.IsNullOrWhiteSpace(track.Lyrics))
            .ToList();
    }

    private static List<ReleaseLink> NormalizeLinks(IEnumerable<ReleaseLink>? links)
    {
        if (links is null)
        {
            return [];
        }

        return links
            .Select(link => new ReleaseLink
            {
                PlatformName = link.PlatformName.Trim(),
                Url = link.Url.Trim()
            })
            .Where(link => !string.IsNullOrWhiteSpace(link.PlatformName) || !string.IsNullOrWhiteSpace(link.Url))
            .ToList();
    }

    private static string NormalizeReleaseTypeOverride(string? releaseTypeOverride)
    {
        return releaseTypeOverride?.Trim() ?? string.Empty;
    }

    private static string GetDisplayReleaseType(Release release)
    {
        if (!string.IsNullOrWhiteSpace(release.ReleaseTypeOverride))
        {
            return release.ReleaseTypeOverride.Trim();
        }

        var trackCount = release.Tracks.Count;
        return trackCount switch
        {
            <= 1 => "Single",
            <= 6 => "EP",
            _ => "Album"
        };
    }
}
