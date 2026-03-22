using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;

namespace AxlProtocolMusic.WebApp.Services.Identity;

public sealed class ReleaseSeedService
{
    private readonly IRepository<Release> _releaseRepository;
    private static readonly DateTimeOffset GeneratedSeedStartDateUtc = new(2026, 3, 21, 0, 0, 0, TimeSpan.Zero);

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
        var releases = new List<Release>
        {
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
        };

        releases.AddRange(GetGeneratedSeedReleases());
        return releases;
    }

    private static IEnumerable<Release> GetGeneratedSeedReleases()
    {
        // These releases are ordered from newest to oldest based on the artist-provided list.
        // Until exact release dates are added, the seed uses a weekly cadence to preserve that order.
        var releaseDateUtc = GeneratedSeedStartDateUtc;

        foreach (var definition in GetMissingReleaseDefinitions())
        {
            yield return CreateSeedRelease(definition.Title, releaseDateUtc, definition.TrackCount);
            releaseDateUtc = releaseDateUtc.AddDays(-7);
        }
    }

    private static IReadOnlyList<SeedReleaseDefinition> GetMissingReleaseDefinitions()
    {
        return
        [
            new("A Step Unknown (Feat. Marcus Sinclair)"),
            new("My Knight In Shining Armor (Feat. Amara Sinclair)"),
            new("I Hate That I Still Think Of You (Metal Version)"),
            new("I Hate That I Still Think Of You"),
            new("St. Patrick's Day 2026", 4),
            new("Christmas Rewired", 9),
            new("The Red-Suited Criminal (2025 Remaster)"),
            new("Grace Renewed", 7),
            new("Double-cross Tango"),
            new("Curse of the Nameless Pharaoh"),
            new("Vengeance Of William Gray"),
            new("Whistle In The Wood"),
            new("The Curse Of Medusa"),
            new("The Transylvanian Twist (2025 Remix)"),
            new("Labor Day", 8),
            new("Vi Al Chupacabra", 2),
            new("Fall of the Djinn"),
            new("Beware The Mimic House (Chillstep Mix)"),
            new("Beware The Mimic House"),
            new("Reclaiming Myself", 2),
            new("My Tragic Love Story", 10),
            new("Middle Ground"),
            new("Deaf Ears, Blind Rage"),
            new("This Is Real"),
            new("The Leprechaun's Rave", 9),
            new("Rosa's Seat"),
            new("The Sky's The Limit (Amelia Earhart)"),
            new("The Scientist's Light (Marie Curie)"),
            new("Failure is Impossible (Tribute to Susan B Anthony)"),
            new("Breaking Codes (Tribute to Ada Lovelace)"),
            new("The Warriors Call (Tribute to Joan of Arc)"),
            new("Rhythm & Heat", 8),
            new("Body Language (Alpha Mix)"),
            new("Calculated Dreams (Tribute to the Hidden Figures: Katherine Johnson, Dorothy Vaughan, and Mary Jackson)"),
            new("A Torch in the Dark (Tribute to Harriet Tubman)"),
            new("The First Chains", 2),
            new("Presidents' Day Rap"),
            new("First Encounters (Original 2025 Release)", 9),
            new("A Kiss at Midnight (Corrected Version)"),
            new("Breaking Point (Metal Version)"),
            new("Breaking Point (Dubstep Version)"),
            new("Let Justice Roll (For MLK day)", 2),
            new("Echoes Of Renewal", 10),
            new("2024 in Review"),
            new("Rap battle: Santa Claus vs. The Grinch"),
            new("The Red-Suited Criminal"),
            new("Resolutions"),
            new("Snowflakes and Whispers"),
            new("Christmas Hustle", 2),
            new("Carol of the Bells (Dubstep Mix)"),
            new("We Wish You a Merry Christmas (Reimagined)"),
            new("Away in a Manger (Country Edition)"),
            new("Deck The Halls (Country Edition)"),
            new("Jingle Bells (Reimagined Country Edition)"),
            new("The Heart of the Season", 10),
            new("Thanksgiving Dance (2024)", 11),
            new("Thanksgiving 2024", 9),
            new("Songs of Thanks and Praise", 7),
            new("Reflections of Love & Gratitude", 5),
            new("Rise of Cthulhu"),
            new("Legend of the Chupacabra"),
            new("The Transylvanian Twist")
        ];
    }

    private static Release CreateSeedRelease(string title, DateTimeOffset releaseDateUtc, int trackCount)
    {
        var slug = CreateSlug(title);

        return new Release
        {
            Id = $"release-{slug}-{releaseDateUtc:yyyy}",
            Title = title,
            Slug = slug,
            ShortDescription = BuildShortDescription(trackCount),
            CoverImageUrl = string.Empty,
            ReleaseTypeOverride = GetReleaseTypeOverride(trackCount),
            ReleaseDateUtc = releaseDateUtc,
            IsPublished = true
        };
    }

    private static string BuildShortDescription(int trackCount)
    {
        return trackCount == 1
            ? "Single by Axl Protocol."
            : $"{trackCount}-track release by Axl Protocol.";
    }

    private static string GetReleaseTypeOverride(int trackCount)
    {
        return trackCount switch
        {
            <= 1 => "Single",
            <= 6 => "EP",
            _ => "Album"
        };
    }

    private static string CreateSlug(string title)
    {
        var slugCharacters = new List<char>();
        var previousWasSeparator = false;

        foreach (var character in title.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                slugCharacters.Add(character);
                previousWasSeparator = false;
                continue;
            }

            if (character is ' ' or '-' or '_' or '&')
            {
                if (!previousWasSeparator && slugCharacters.Count > 0)
                {
                    slugCharacters.Add('-');
                    previousWasSeparator = true;
                }
            }
        }

        return new string([.. slugCharacters]).Trim('-');
    }

    private sealed record SeedReleaseDefinition(string Title, int TrackCount = 1);
}
