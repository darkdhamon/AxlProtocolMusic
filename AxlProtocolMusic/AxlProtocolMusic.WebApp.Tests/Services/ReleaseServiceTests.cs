using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class ReleaseServiceTests
{
    [Test]
    public async Task GetFeaturedReleasesAsync_WhenRecentPublishedCountIsLessThanThree_FallsBackToLatestPublished()
    {
        var repository = new InMemoryReleaseRepository(
        [
            CreateRelease("oldest", daysAgo: 120, isPublished: true),
            CreateRelease("older", daysAgo: 45, isPublished: true),
            CreateRelease("recent", daysAgo: 10, isPublished: true),
            CreateRelease("draft", daysAgo: 1, isPublished: false)
        ]);

        var service = new ReleaseService(repository);

        var result = await service.GetFeaturedReleasesAsync();

        Assert.That(result.Select(item => item.Slug), Is.EqualTo(new[] { "recent", "older", "oldest" }));
    }

    [Test]
    public async Task GetPagedReleasesAsync_TrimsSearchTerm_ClampsPaging_AndFiltersPublishedReleases()
    {
        var repository = new InMemoryReleaseRepository(
        [
            CreateRelease("alpha", 30, true, title: "Alpha One", shortDescription: "first"),
            CreateRelease("beta", 20, true, title: "Beta Song", shortDescription: "second"),
            CreateRelease("gamma", 10, true, title: "Gamma Song", shortDescription: "third"),
            CreateRelease("draft-song", 5, false, title: "Draft Song", shortDescription: "hidden")
        ]);

        var service = new ReleaseService(repository);

        var result = await service.GetPagedReleasesAsync("  song  ", pageNumber: 5, pageSize: 1);

        Assert.That(result.SearchTerm, Is.EqualTo("song"));
        Assert.That(result.TotalCount, Is.EqualTo(2));
        Assert.That(result.TotalPages, Is.EqualTo(2));
        Assert.That(result.PageNumber, Is.EqualTo(2));
        Assert.That(result.PageSize, Is.EqualTo(1));
        Assert.That(result.Items.Select(item => item.Slug), Is.EqualTo(new[] { "beta" }));
    }

    [Test]
    public async Task GetReleaseBySlugAsync_WhenLegacyLyricsExist_MigratesLyricsIntoSingleTrackAndUpdatesRepository()
    {
        var release = CreateRelease("legacy-release", 3, true, title: "Legacy Release");
        release.Lyrics = "  Legacy lyrics body  ";
        release.Tracks = [];

        var repository = new InMemoryReleaseRepository([release]);
        var service = new ReleaseService(repository);

        var result = await service.GetReleaseBySlugAsync("LEGACY-RELEASE");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Tracks, Has.Count.EqualTo(1));
        Assert.That(result.Tracks[0].Title, Is.EqualTo("Legacy Release"));
        Assert.That(result.Tracks[0].Lyrics, Is.EqualTo("Legacy lyrics body"));
        Assert.That(repository.UpdatedDocuments, Has.Count.EqualTo(1));
        Assert.That(repository.Documents[0].Lyrics, Is.Empty);
    }

    [Test]
    public async Task GetReleaseBySlugAsync_WhenReleaseTypeOverrideIsBlank_UsesTrackCountToChooseReleaseType()
    {
        var release = CreateRelease("ep-release", 4, true);
        release.Tracks =
        [
            new ReleaseTrack { Title = "Track 1" },
            new ReleaseTrack { Title = "Track 2" },
            new ReleaseTrack { Title = "Track 3" }
        ];

        var service = new ReleaseService(new InMemoryReleaseRepository([release]));

        var result = await service.GetReleaseBySlugAsync("ep-release");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ReleaseType, Is.EqualTo("EP"));
    }

    [Test]
    public async Task CreateReleaseAsync_WhenSlugAlreadyExists_ReturnsError()
    {
        var repository = new InMemoryReleaseRepository([CreateRelease("existing-slug", 2, true)]);
        var service = new ReleaseService(repository);

        var result = await service.CreateReleaseAsync(new ReleaseUpdateRequest
        {
            Title = "New release",
            Slug = " Existing-Slug ",
            ShortDescription = "Description",
            ReleaseDate = new DateTime(2026, 3, 21)
        });

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("That release slug is already in use."));
        Assert.That(repository.CreatedDocuments, Is.Empty);
    }

    [Test]
    public async Task CreateReleaseAsync_NormalizesFieldsBeforePersisting()
    {
        var repository = new InMemoryReleaseRepository([]);
        var service = new ReleaseService(repository);

        var result = await service.CreateReleaseAsync(new ReleaseUpdateRequest
        {
            Title = "  New Release  ",
            Slug = " New Release ",
            ShortDescription = "  Short description  ",
            CoverImageUrl = "  /uploads/releases/cover.png  ",
            Story = "  Story body  ",
            ReleaseTypeOverride = "  Mixtape  ",
            Tags = [" TagOne ", "tagone", "  ", "TagTwo"],
            Credits =
            [
                new ReleaseCredit { Name = "  Artist One  ", Roles = [" Vocals ", "vocals", " "] },
                new ReleaseCredit { Name = " ", Roles = [] }
            ],
            Tracks =
            [
                new ReleaseTrack { Title = "  Intro  ", Duration = " 1:00 ", Lyrics = "  hi  " },
                new ReleaseTrack()
            ],
            Links =
            [
                new ReleaseLink { PlatformName = " Spotify ", Url = " https://example.test " },
                new ReleaseLink()
            ],
            ReleaseDate = new DateTime(2026, 3, 21),
            IsPublished = true
        });

        Assert.That(result.Succeeded, Is.True);
        Assert.That(repository.CreatedDocuments, Has.Count.EqualTo(1));

        var created = repository.CreatedDocuments.Single();
        Assert.That(created.Title, Is.EqualTo("New Release"));
        Assert.That(created.Slug, Is.EqualTo("new release"));
        Assert.That(created.ShortDescription, Is.EqualTo("Short description"));
        Assert.That(created.CoverImageUrl, Is.EqualTo("/uploads/releases/cover.png"));
        Assert.That(created.Story, Is.EqualTo("Story body"));
        Assert.That(created.ReleaseTypeOverride, Is.EqualTo("Mixtape"));
        Assert.That(created.Tags, Is.EqualTo(new[] { "TagOne", "TagTwo" }));
        Assert.That(created.Credits, Has.Count.EqualTo(1));
        Assert.That(created.Credits[0].Name, Is.EqualTo("Artist One"));
        Assert.That(created.Credits[0].Roles, Is.EqualTo(new[] { "Vocals" }));
        Assert.That(created.Tracks, Has.Count.EqualTo(1));
        Assert.That(created.Tracks[0].Title, Is.EqualTo("Intro"));
        Assert.That(created.Tracks[0].Duration, Is.EqualTo("1:00"));
        Assert.That(created.Tracks[0].Lyrics, Is.EqualTo("hi"));
        Assert.That(created.Links, Has.Count.EqualTo(1));
        Assert.That(created.Links[0].PlatformName, Is.EqualTo("Spotify"));
        Assert.That(created.Links[0].Url, Is.EqualTo("https://example.test"));
        Assert.That(created.ReleaseDateUtc, Is.EqualTo(new DateTimeOffset(new DateTime(2026, 3, 21), TimeSpan.Zero)));
        Assert.That(created.IsPublished, Is.True);
    }

    [Test]
    public async Task UpdateReleaseAsync_WhenSlugConflictsWithAnotherRelease_ReturnsError()
    {
        var repository = new InMemoryReleaseRepository(
        [
            CreateRelease("original", 15, true, id: "one"),
            CreateRelease("taken", 10, true, id: "two")
        ]);

        var service = new ReleaseService(repository);

        var result = await service.UpdateReleaseAsync(new ReleaseUpdateRequest
        {
            OriginalSlug = "original",
            Title = "Updated",
            Slug = "Taken",
            ShortDescription = "Updated",
            ReleaseDate = new DateTime(2026, 3, 21)
        });

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("That release slug is already in use."));
        Assert.That(repository.UpdatedDocuments, Is.Empty);
    }

    [Test]
    public async Task UpdateReleaseAsync_WhenSuccessful_NormalizesAndPersistsChanges()
    {
        var existing = CreateRelease("original", 20, true, id: "release-1", title: "Original");
        existing.CoverImageUrl = "/uploads/releases/original.png";

        var repository = new InMemoryReleaseRepository([existing]);
        var service = new ReleaseService(repository);

        var result = await service.UpdateReleaseAsync(new ReleaseUpdateRequest
        {
            OriginalSlug = "ORIGINAL",
            Title = "  Updated Release  ",
            Slug = " Updated Release ",
            ShortDescription = "  Updated description ",
            CoverImageUrl = "  /uploads/releases/updated.png ",
            Story = "  Updated story ",
            ReleaseTypeOverride = "  Album  ",
            Tags = [" Rock ", "rock", "Live"],
            Credits = [new ReleaseCredit { Name = "  Producer  ", Roles = [" Mix ", "mix"] }],
            Tracks = [new ReleaseTrack { Title = "  Song  ", Duration = " 3:33 ", Lyrics = "  words " }],
            Links = [new ReleaseLink { PlatformName = " Bandcamp ", Url = " https://bandcamp.test " }],
            ReleaseDate = new DateTime(2026, 3, 22),
            IsPublished = false
        });

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Slug, Is.EqualTo("updated release"));
        Assert.That(repository.UpdatedDocuments, Has.Count.EqualTo(1));

        var updated = repository.Documents.Single();
        Assert.That(updated.Title, Is.EqualTo("Updated Release"));
        Assert.That(updated.Slug, Is.EqualTo("updated release"));
        Assert.That(updated.ShortDescription, Is.EqualTo("Updated description"));
        Assert.That(updated.CoverImageUrl, Is.EqualTo("/uploads/releases/updated.png"));
        Assert.That(updated.Story, Is.EqualTo("Updated story"));
        Assert.That(updated.ReleaseTypeOverride, Is.EqualTo("Album"));
        Assert.That(updated.Tags, Is.EqualTo(new[] { "Rock", "Live" }));
        Assert.That(updated.Credits[0].Name, Is.EqualTo("Producer"));
        Assert.That(updated.Credits[0].Roles, Is.EqualTo(new[] { "Mix" }));
        Assert.That(updated.Tracks[0].Title, Is.EqualTo("Song"));
        Assert.That(updated.Tracks[0].Lyrics, Is.EqualTo("words"));
        Assert.That(updated.Links[0].PlatformName, Is.EqualTo("Bandcamp"));
        Assert.That(updated.IsPublished, Is.False);
    }

    [Test]
    public async Task DeleteReleaseAsync_WhenReleaseExists_DeletesItAndReturnsManagedImagePath()
    {
        var existing = CreateRelease("delete-me", 2, true, id: "release-1", title: "Delete Me");
        existing.CoverImageUrl = "/uploads/releases/delete-me.png";

        var repository = new InMemoryReleaseRepository([existing]);
        var service = new ReleaseService(repository);

        var result = await service.DeleteReleaseAsync("DELETE-ME");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ImageStoragePath, Is.EqualTo("/uploads/releases/delete-me.png"));
        Assert.That(repository.Documents, Is.Empty);
    }

    [Test]
    public async Task GenerateUniqueSlugAsync_WhenBaseAndDateSlugExist_AppendsIncrementingSuffix()
    {
        var today = DateTimeOffset.UtcNow.ToString("yyMMdd");
        var repository = new InMemoryReleaseRepository(
        [
            CreateRelease("my-release", 10, true),
            CreateRelease($"my-release-{today}", 9, true),
            CreateRelease($"my-release-{today}-2", 8, true)
        ]);

        var service = new ReleaseService(repository);

        var result = await service.GenerateUniqueSlugAsync("My Release");

        Assert.That(result, Is.EqualTo($"my-release-{today}-3"));
    }

    [Test]
    public async Task GetKnownMetadataHelpers_ReturnDistinctSortedValues()
    {
        var repository = new InMemoryReleaseRepository(
        [
            new Release
            {
                Id = "one",
                Title = "One",
                Slug = "one",
                ShortDescription = "First",
                ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-3),
                IsPublished = true,
                Credits =
                [
                    new ReleaseCredit { Name = " Alice ", Roles = [" Vocals ", "vocals"] },
                    new ReleaseCredit { Name = "Bob", Roles = [" Guitar "] }
                ],
                Tags = [" Rock ", "live"]
            },
            new Release
            {
                Id = "two",
                Title = "Two",
                Slug = "two",
                ShortDescription = "Second",
                ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-2),
                IsPublished = true,
                Credits = [new ReleaseCredit { Name = "alice", Roles = [" Production "] }],
                Tags = ["Live", " Acoustic "]
            }
        ]);

        var service = new ReleaseService(repository);

        var roles = await service.GetKnownCreditRolesAsync();
        var contributors = await service.GetKnownContributorNamesAsync();
        var tags = await service.GetKnownTagsAsync();

        Assert.That(roles, Is.EqualTo(new[] { "Guitar", "Production", "Vocals" }));
        Assert.That(contributors, Is.EqualTo(new[] { "Alice", "Bob" }));
        Assert.That(tags, Is.EqualTo(new[] { "Acoustic", "live", "Rock" }));
    }

    [Test]
    public void IsManagedImageUrl_ReturnsTrueOnlyForUploadsPaths()
    {
        var service = new ReleaseService(new InMemoryReleaseRepository([]));

        Assert.Multiple(() =>
        {
            Assert.That(service.IsManagedImageUrl("/uploads/releases/image.png"), Is.True);
            Assert.That(service.IsManagedImageUrl("/images/releases/image.png"), Is.False);
            Assert.That(service.IsManagedImageUrl(""), Is.False);
            Assert.That(service.IsManagedImageUrl(null), Is.False);
        });
    }

    private static Release CreateRelease(
        string slug,
        int daysAgo,
        bool isPublished,
        string? id = null,
        string? title = null,
        string? shortDescription = null)
    {
        return new Release
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            Title = title ?? slug,
            Slug = slug,
            ShortDescription = shortDescription ?? $"{slug} description",
            CoverImageUrl = $"/images/{slug}.png",
            Story = $"{slug} story",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-daysAgo),
            IsPublished = isPublished
        };
    }

    private sealed class InMemoryReleaseRepository : IRepository<Release>
    {
        public InMemoryReleaseRepository(IEnumerable<Release> documents)
        {
            Documents = documents.ToList();
        }

        public List<Release> Documents { get; }

        public List<Release> CreatedDocuments { get; } = [];

        public List<Release> UpdatedDocuments { get; } = [];

        public Task CreateAsync(Release document, CancellationToken cancellationToken = default)
        {
            CreatedDocuments.Add(document);
            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            Documents.RemoveAll(document => string.Equals(document.Id, id, StringComparison.Ordinal));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Release>> FindAsync(Expression<Func<Release, bool>> filter, CancellationToken cancellationToken = default)
        {
            var predicate = filter.Compile();
            return Task.FromResult<IReadOnlyList<Release>>(Documents.Where(predicate).ToList());
        }

        public Task<IReadOnlyList<Release>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Release>>(Documents.ToList());

        public Task<Release?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Documents.FirstOrDefault(document => string.Equals(document.Id, id, StringComparison.Ordinal)));

        public Task UpdateAsync(Release document, CancellationToken cancellationToken = default)
        {
            UpdatedDocuments.Add(document);
            return Task.CompletedTask;
        }
    }
}
