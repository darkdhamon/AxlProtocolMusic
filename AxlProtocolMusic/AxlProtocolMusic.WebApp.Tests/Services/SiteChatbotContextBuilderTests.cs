using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class SiteChatbotContextBuilderTests
{
    [Test]
    public async Task BuildAsync_IncludesCoreSectionsAndForwardsExpectedReleaseQuery()
    {
        var aboutService = new FakeAboutPageService
        {
            Content = new AboutPageContent
            {
                HeroLead = "Hero lead",
                HeroBody = "Hero body",
                FocusPoints = ["One", "Two"],
                NarrativeHighlights = ["Highlight A", "Highlight B"],
                Pillars =
                [
                    new AboutPillar { Title = "Pillar One", Description = "First pillar description" }
                ]
            }
        };

        var releaseService = new FakeReleaseService
        {
            PageResult = new PagedReleaseResult
            {
                Items =
                [
                    new ReleaseListItemViewModel
                    {
                        Title = "Release One",
                        Slug = "release-one",
                        ShortDescription = "First release summary",
                        ReleaseDateUtc = new DateTimeOffset(2026, 3, 20, 0, 0, 0, TimeSpan.Zero)
                    }
                ]
            }
        };

        releaseService.ReleaseDetailsBySlug["release-one"] = new ReleaseDetailsViewModel
        {
            Title = "Release One",
            Slug = "release-one",
            ReleaseType = "Album",
            Tags = ["Tag1", "Tag2"],
            Credits = [new ReleaseCredit { Name = "Artist", Roles = ["Vocals", "Production"] }],
            Tracks = [new ReleaseTrack { Title = "Track One", Duration = "3:21" }],
            Story = "Story body"
        };

        var newsService = new FakeNewsArticleService
        {
            Articles =
            [
                new NewsArticle
                {
                    Id = "news-1",
                    Title = "News One",
                    Content = "News preview content",
                    PublicationDateUtc = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero)
                }
            ]
        };

        var timelineService = new FakeTimelineEventService
        {
            Events =
            [
                new TimelineEvent
                {
                    Id = "event-1",
                    Title = "Timeline Event",
                    EventDateUtc = new DateTimeOffset(2026, 3, 18, 0, 0, 0, TimeSpan.Zero),
                    EventType = TimelineEventType.Release,
                    ShortDescription = "Timeline summary"
                }
            ]
        };

        var builder = new SiteChatbotContextBuilder(aboutService, releaseService, newsService, timelineService);
        using var cancellationSource = new CancellationTokenSource();

        var result = await builder.BuildAsync(cancellationSource.Token);

        Assert.That(releaseService.LastSearchTerm, Is.Null);
        Assert.That(releaseService.LastPageNumber, Is.EqualTo(1));
        Assert.That(releaseService.LastPageSize, Is.EqualTo(250));
        Assert.That(releaseService.LastIncludeUnpublished, Is.False);
        Assert.That(releaseService.LastCancellationToken, Is.EqualTo(cancellationSource.Token));
        Assert.That(newsService.LastIncludeUnpublished, Is.False);
        Assert.That(aboutService.LastCancellationToken, Is.EqualTo(cancellationSource.Token));
        Assert.That(timelineService.LastCancellationToken, Is.EqualTo(cancellationSource.Token));

        Assert.That(result, Does.Contain("Site navigation:"));
        Assert.That(result, Does.Contain("- Home: /"));
        Assert.That(result, Does.Contain("About Axl Protocol:"));
        Assert.That(result, Does.Contain("- Hero lead: Hero lead"));
        Assert.That(result, Does.Contain("- Focus points: One | Two"));
        Assert.That(result, Does.Contain("- Pillar: Pillar One - First pillar description"));
        Assert.That(result, Does.Contain("Published releases:"));
        Assert.That(result, Does.Contain("- Release One | 2026-03-20 | /releases/release-one"));
        Assert.That(result, Does.Contain("Detailed release notes:"));
        Assert.That(result, Does.Contain("  Type: Album"));
        Assert.That(result, Does.Contain("  Tags: Tag1, Tag2"));
        Assert.That(result, Does.Contain("  Credits: Artist (Vocals, Production)"));
        Assert.That(result, Does.Contain("  Tracks: Track One [3:21]"));
        Assert.That(result, Does.Contain("Recent news articles:"));
        Assert.That(result, Does.Contain("- News One | 2026-03-19 | /news"));
        Assert.That(result, Does.Contain("Timeline events:"));
        Assert.That(result, Does.Contain("- Timeline Event | 2026-03-18 | /timeline"));
    }

    [Test]
    public async Task BuildAsync_AppliesLimitsTruncationAndSkipsMissingReleaseDetails()
    {
        var aboutService = new FakeAboutPageService
        {
            Content = new AboutPageContent
            {
                HeroLead = "  " + new string('L', 300) + "  ",
                HeroBody = "Line one\r\nLine two",
                FocusPoints = Enumerable.Range(1, 7).Select(index => $"Focus {index}").ToList(),
                NarrativeHighlights = Enumerable.Range(1, 7).Select(index => $"Highlight {index}").ToList(),
                Pillars = Enumerable.Range(1, 6)
                    .Select(index => new AboutPillar
                    {
                        Title = $"Pillar {index}",
                        Description = $"Description {index}"
                    })
                    .ToList()
            }
        };

        var releases = Enumerable.Range(1, 20)
            .Select(index => new ReleaseListItemViewModel
            {
                Title = $"Release {index}",
                Slug = $"release-{index}",
                ShortDescription = $"Summary {index}",
                ReleaseDateUtc = new DateTimeOffset(2026, 3, index <= 28 ? index : 28, 0, 0, 0, TimeSpan.Zero)
            })
            .ToList();

        var releaseService = new FakeReleaseService
        {
            PageResult = new PagedReleaseResult
            {
                Items = releases
            }
        };

        for (var index = 1; index <= 8; index++)
        {
            if (index == 3)
            {
                releaseService.ReleaseDetailsBySlug[$"release-{index}"] = null;
                continue;
            }

            releaseService.ReleaseDetailsBySlug[$"release-{index}"] = new ReleaseDetailsViewModel
            {
                Title = $"Release {index}",
                Slug = $"release-{index}",
                ReleaseType = "EP",
                Tags = index % 2 == 0 ? [] : ["TagA", "TagB"],
                Credits = index == 1
                    ? Enumerable.Range(1, 8)
                        .Select(creditIndex => new ReleaseCredit
                        {
                            Name = $"Credit {creditIndex}",
                            Roles = ["Role1", "Role2", "Role3", "Role4"]
                        })
                        .ToList()
                    : [],
                Tracks = index == 1
                    ? Enumerable.Range(1, 10)
                        .Select(trackIndex => new ReleaseTrack
                        {
                            Title = $"Track {trackIndex}",
                            Duration = trackIndex % 2 == 0 ? string.Empty : "4:00"
                        })
                        .ToList()
                    : [],
                Story = index == 1 ? new string('S', 300) : string.Empty
            };
        }

        var newsService = new FakeNewsArticleService
        {
            Articles = Enumerable.Range(1, 10)
                .Select(index => new NewsArticle
                {
                    Id = $"news-{index}",
                    Title = $"News {index}",
                    Content = index == 1 ? "  News line one\r\nNews line two  " : $"Content {index}",
                    PublicationDateUtc = new DateTimeOffset(2026, 2, index <= 28 ? index : 28, 0, 0, 0, TimeSpan.Zero)
                })
                .ToList()
        };

        var timelineService = new FakeTimelineEventService
        {
            Events = Enumerable.Range(1, 12)
                .Select(index => new TimelineEvent
                {
                    Id = $"event-{index}",
                    Title = $"Event {index}",
                    EventDateUtc = new DateTimeOffset(2026, 1, index <= 28 ? index : 28, 0, 0, 0, TimeSpan.Zero),
                    EventType = TimelineEventType.Milestone,
                    ShortDescription = index == 1 ? "  Timeline line one\r\nTimeline line two  " : $"Summary {index}"
                })
                .ToList()
        };

        var builder = new SiteChatbotContextBuilder(aboutService, releaseService, newsService, timelineService);

        var result = await builder.BuildAsync();

        Assert.That(releaseService.DetailRequests, Has.Count.EqualTo(8));
        Assert.That(releaseService.DetailRequests, Is.EqualTo(Enumerable.Range(1, 8).Select(index => $"release-{index}")));

        Assert.That(CountOccurrences(result, "- Release: "), Is.EqualTo(7));
        Assert.That(CountOccurrences(result, "Published releases:"), Is.EqualTo(1));
        Assert.That(CountOccurrences(result, "Recent news articles:"), Is.EqualTo(1));
        Assert.That(CountOccurrences(result, "Timeline events:"), Is.EqualTo(1));
        Assert.That(CountOccurrences(result, " | /releases/release-"), Is.EqualTo(18));
        Assert.That(CountOccurrences(result, " | /news"), Is.EqualTo(8));
        Assert.That(CountOccurrences(result, " | /timeline"), Is.EqualTo(10));

        Assert.That(result, Does.Contain(new string('L', 260) + "..."));
        Assert.That(result, Does.Contain("- Hero body: Line one  Line two"));
        Assert.That(result, Does.Contain("- Focus points: Focus 1 | Focus 2 | Focus 3 | Focus 4 | Focus 5"));
        Assert.That(result, Does.Not.Contain("Focus 6"));
        Assert.That(result, Does.Contain("- Narrative highlights: Highlight 1 | Highlight 2 | Highlight 3 | Highlight 4 | Highlight 5"));
        Assert.That(result, Does.Not.Contain("Highlight 6"));
        Assert.That(result, Does.Contain("- Pillar: Pillar 4 - Description 4"));
        Assert.That(result, Does.Not.Contain("Pillar 5"));
        Assert.That(result, Does.Not.Contain("Release 19 |"));
        Assert.That(result, Does.Not.Contain("Release 20 |"));
        Assert.That(result, Does.Not.Contain("- Release: Release 3"));
        Assert.That(result, Does.Contain("  Tags: none"));
        Assert.That(result, Does.Contain("  Credits: Credit 1 (Role1, Role2, Role3)"));
        Assert.That(result, Does.Not.Contain("Credit 7"));
        Assert.That(result, Does.Contain("  Tracks: Track 1 [4:00] | Track 2 | Track 3 [4:00]"));
        Assert.That(result, Does.Not.Contain("Track 9"));
        Assert.That(result, Does.Contain("  Story: " + new string('S', 260) + "..."));
        Assert.That(result, Does.Contain("  Preview: News line one  News line two"));
        Assert.That(result, Does.Contain("  Summary: Timeline line one  Timeline line two"));
        Assert.That(result, Does.Not.Contain("- News 9 |"));
        Assert.That(result, Does.Not.Contain("- Event 11 |"));
    }

    private static int CountOccurrences(string value, string fragment)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += fragment.Length;
        }

        return count;
    }

    private sealed class FakeAboutPageService : IAboutPageService
    {
        public AboutPageContent Content { get; set; } = new();

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<AboutPageContent> GetAsync(CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Content);
        }

        public Task UpdateAsync(AboutPageContent content, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SeedAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeReleaseService : IReleaseService
    {
        public PagedReleaseResult PageResult { get; set; } = new();

        public Dictionary<string, ReleaseDetailsViewModel?> ReleaseDetailsBySlug { get; } = [];

        public List<string> DetailRequests { get; } = [];

        public string? LastSearchTerm { get; private set; }

        public int LastPageNumber { get; private set; }

        public int LastPageSize { get; private set; }

        public bool LastIncludeUnpublished { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PagedReleaseResult> GetPagedReleasesAsync(string? searchTerm, int pageNumber, int pageSize, bool includeUnpublished = false, CancellationToken cancellationToken = default)
        {
            LastSearchTerm = searchTerm;
            LastPageNumber = pageNumber;
            LastPageSize = pageSize;
            LastIncludeUnpublished = includeUnpublished;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(PageResult);
        }

        public Task<ReleaseDetailsViewModel?> GetReleaseBySlugAsync(string slug, bool includeUnpublished = false, CancellationToken cancellationToken = default)
        {
            DetailRequests.Add(slug);
            return Task.FromResult(ReleaseDetailsBySlug.TryGetValue(slug, out var details) ? details : null);
        }

        public Task<ReleaseUpdateResult> UpdateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ReleaseCreateResult> CreateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ReleaseDeleteResult> DeleteReleaseAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<string> GenerateUniqueSlugAsync(string? value, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> GetKnownCreditRolesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> GetKnownContributorNamesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetKnownContributorRolesByNameAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> GetKnownTagsAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public bool IsManagedImageUrl(string? imageUrl)
            => throw new NotSupportedException();
    }

    private sealed class FakeNewsArticleService : INewsArticleService
    {
        public IReadOnlyList<NewsArticle> Articles { get; set; } = [];

        public bool LastIncludeUnpublished { get; private set; }

        public Task<IReadOnlyList<NewsArticle>> GetArticlesAsync(bool includeUnpublished = false, CancellationToken cancellationToken = default)
        {
            LastIncludeUnpublished = includeUnpublished;
            return Task.FromResult(Articles);
        }

        public Task<NewsArticle> CreateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<NewsArticle> UpdateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public bool IsManagedImageUrl(string? imageUrl)
            => throw new NotSupportedException();
    }

    private sealed class FakeTimelineEventService : ITimelineEventService
    {
        public IReadOnlyList<TimelineEvent> Events { get; set; } = [];

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<IReadOnlyList<TimelineEvent>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Events);
        }

        public Task CreateAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SeedAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
