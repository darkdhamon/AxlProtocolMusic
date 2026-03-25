using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class TimelinePageTests
{
    [Test]
    public void Timeline_WhenEntriesExist_RendersCombinedTimeline()
    {
        using var context = new BunitContext();
        var releaseService = new FakeReleaseService
        {
            Result = new PagedReleaseResult
            {
                Items =
                [
                    new ReleaseListItemViewModel
                    {
                        Title = "Signals",
                        Slug = "signals",
                        ShortDescription = "Release copy",
                        ReleaseDateUtc = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
                        CoverImageUrl = string.Empty,
                        IsPublished = true
                    }
                ]
            }
        };
        var newsService = new FakeNewsArticleService
        {
            Articles =
            [
                new NewsArticle
                {
                    Id = "news-1",
                    Title = "Studio Update",
                    Slug = "studio-update",
                    Content = "One two three four five six seven eight nine ten.",
                    PublicationDateUtc = new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero),
                    IsPublished = true
                }
            ]
        };
        var timelineEventService = new FakeTimelineEventService
        {
            Events =
            [
                new TimelineEvent
                {
                    Id = "event-1",
                    Title = "Project Began",
                    ShortDescription = "A manual milestone.",
                    EventDateUtc = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
                    EventType = TimelineEventType.Milestone
                }
            ]
        };

        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.AddAuthorization().SetNotAuthorized();
        context.Services.AddSingleton<IReleaseService>(releaseService);
        context.Services.AddSingleton<INewsArticleService>(newsService);
        context.Services.AddSingleton<ITimelineEventService>(timelineEventService);

        var cut = context.Render<Timeline>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("The Axl Protocol Timeline"));
            Assert.That(cut.Markup, Does.Contain("Signals"));
            Assert.That(cut.Markup, Does.Contain("Studio Update"));
            Assert.That(cut.Markup, Does.Contain("Project Began"));
            Assert.That(cut.Markup, Does.Contain("View Release"));
            Assert.That(cut.Markup, Does.Contain("View Article"));
            Assert.That(cut.Markup, Does.Contain("All timeline months are loaded."));
        });
    }

    private sealed class FakeReleaseService : IReleaseService
    {
        public PagedReleaseResult Result { get; set; } = new();

        public Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FeaturedReleaseViewModel>>([]);

        public Task<PagedReleaseResult> GetPagedReleasesAsync(string? searchTerm, int pageNumber, int pageSize, bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Result);

        public Task<ReleaseDetailsViewModel?> GetReleaseBySlugAsync(string slug, bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => Task.FromResult<ReleaseDetailsViewModel?>(null);

        public Task<ReleaseUpdateResult> UpdateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseUpdateResult());

        public Task<ReleaseCreateResult> CreateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseCreateResult());

        public Task<ReleaseDeleteResult> DeleteReleaseAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseDeleteResult());

        public Task<string> GenerateUniqueSlugAsync(string? value, CancellationToken cancellationToken = default)
            => Task.FromResult(value ?? string.Empty);

        public Task<IReadOnlyList<string>> GetKnownCreditRolesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<string>> GetKnownContributorNamesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<string>> GetKnownTagsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public bool IsManagedImageUrl(string? imageUrl) => false;
    }

    private sealed class FakeNewsArticleService : INewsArticleService
    {
        public IReadOnlyList<NewsArticle> Articles { get; set; } = [];

        public Task<IReadOnlyList<NewsArticle>> GetArticlesAsync(bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Articles);

        public Task<NewsArticle> CreateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new NewsArticle());

        public Task<NewsArticle> UpdateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new NewsArticle());

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public bool IsManagedImageUrl(string? imageUrl) => false;
    }

    private sealed class FakeTimelineEventService : ITimelineEventService
    {
        public IReadOnlyList<TimelineEvent> Events { get; set; } = [];

        public Task<IReadOnlyList<TimelineEvent>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Events);

        public Task CreateAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SeedAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
