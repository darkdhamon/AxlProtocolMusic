using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class TimelinePageTests
{
    [Test]
    public void Timeline_WhenEntriesExist_RendersCombinedTimeline()
    {
        using var context = CreateContext(out var releaseService, out var newsService, out var timelineEventService);
        context.AddAuthorization().SetNotAuthorized();
        releaseService.Result = new PagedReleaseResult
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
        };
        newsService.Articles =
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
        ];
        timelineEventService.Events =
        [
            new TimelineEvent
            {
                Id = "event-1",
                Title = "Project Began",
                ShortDescription = "A manual milestone.",
                EventDateUtc = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
                EventType = TimelineEventType.Milestone
            }
        ];

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

    [Test]
    public void Timeline_WhenEditorQueryParameterIsPresent_OpensCreateModal()
    {
        using var context = CreateContext(out _, out _, out _);
        context.AddAuthorization().SetAuthorized("admin");
        var navigationManager = context.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/timeline?editor=new");

        var cut = context.Render<Timeline>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Edit Timeline Event"));
            Assert.That(cut.Markup, Does.Contain("Create Event"));
            Assert.That(cut.Find("#timeline-title").GetAttribute("value"), Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void Timeline_WhenCreateSucceeds_PersistsEventReloadsTimelineAndRemovesEditorQueryParameter()
    {
        using var context = CreateContext(out _, out _, out var timelineEventService);
        context.AddAuthorization().SetAuthorized("admin");
        var navigationManager = context.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/timeline?editor=new");

        var cut = context.Render<Timeline>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Create Event"));
        });

        cut.Find("#timeline-title").Input("Festival Debut");
        cut.Find("#timeline-description").Input("First public performance.");
        cut.Find("#timeline-image").Input("https://cdn.example/festival.jpg");
        cut.Find("#timeline-date").Change("2026-04-09");
        cut.Find("#timeline-type").Change(TimelineEventType.Release.ToString());
        cut.Find("button.btn.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(timelineEventService.CreatedEvents, Has.Count.EqualTo(1));
            Assert.That(cut.Markup, Does.Contain("Festival Debut"));
            Assert.That(cut.Markup, Does.Not.Contain("Edit Timeline Event"));
            Assert.That(navigationManager.Uri, Does.EndWith("/timeline"));
        });

        var createdEvent = timelineEventService.CreatedEvents.Single();
        Assert.Multiple(() =>
        {
            Assert.That(createdEvent.Title, Is.EqualTo("Festival Debut"));
            Assert.That(createdEvent.ShortDescription, Is.EqualTo("First public performance."));
            Assert.That(createdEvent.ImageUrl, Is.EqualTo("https://cdn.example/festival.jpg"));
            Assert.That(createdEvent.EventType, Is.EqualTo(TimelineEventType.Release));
            Assert.That(createdEvent.EventDateUtc, Is.EqualTo(new DateTimeOffset(2026, 4, 9, 0, 0, 0, TimeSpan.Zero)));
        });
    }

    [Test]
    public void Timeline_WhenCreateFails_ShowsServiceErrorAndKeepsModalOpen()
    {
        using var context = CreateContext(out _, out _, out var timelineEventService);
        context.AddAuthorization().SetAuthorized("admin");
        timelineEventService.CreateException = new InvalidOperationException("Timeline save failed.");
        var navigationManager = context.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/timeline?editor=new");

        var cut = context.Render<Timeline>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Create Event"));
        });

        cut.Find("#timeline-title").Input("Festival Debut");
        cut.Find("#timeline-description").Input("First public performance.");
        cut.Find("button.btn.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Timeline save failed."));
            Assert.That(cut.Markup, Does.Contain("Edit Timeline Event"));
        });

        Assert.That(timelineEventService.CreatedEvents, Has.Count.EqualTo(1));
        Assert.That(navigationManager.Uri, Does.EndWith("/timeline?editor=new"));
    }

    [Test]
    public void Timeline_WhenAdminDeletesManualEvent_RemovesEventFromTimeline()
    {
        using var context = CreateContext(out _, out _, out var timelineEventService);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        timelineEventService.Events =
        [
            new TimelineEvent
            {
                Id = "event-1",
                Title = "Project Began",
                ShortDescription = "A manual milestone.",
                EventDateUtc = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
                EventType = TimelineEventType.Milestone
            }
        ];

        var cut = context.Render<Timeline>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Project Began"));
            Assert.That(cut.Markup, Does.Contain("Edit Event"));
        });

        cut.Find("button.btn.btn-outline-danger.btn-sm").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Delete Timeline Event"));
        });

        cut.Find("button.btn.btn-danger").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(timelineEventService.DeletedIds, Is.EqualTo(["event-1"]));
            Assert.That(cut.Markup, Does.Not.Contain("Project Began"));
            Assert.That(cut.FindAll(".event-card"), Is.Empty);
            Assert.That(cut.FindAll(".stat-number").First().TextContent, Is.EqualTo("0"));
        });
    }

    private static BunitContext CreateContext(
        out FakeReleaseService releaseService,
        out FakeNewsArticleService newsService,
        out FakeTimelineEventService timelineEventService)
    {
        var context = new BunitContext();
        releaseService = new FakeReleaseService();
        newsService = new FakeNewsArticleService();
        timelineEventService = new FakeTimelineEventService();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton<IReleaseService>(releaseService);
        context.Services.AddSingleton<INewsArticleService>(newsService);
        context.Services.AddSingleton<ITimelineEventService>(timelineEventService);
        return context;
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
        public List<TimelineEvent> Events { get; set; } = [];

        public List<TimelineEvent> CreatedEvents { get; } = [];

        public List<TimelineEvent> UpdatedEvents { get; } = [];

        public List<string> DeletedIds { get; } = [];

        public Exception? CreateException { get; set; }

        public Task<IReadOnlyList<TimelineEvent>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TimelineEvent>>(Events.ToList());

        public Task CreateAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default)
        {
            CreatedEvents.Add(CloneTimelineEvent(timelineEvent));
            if (CreateException is not null)
            {
                throw CreateException;
            }

            var storedEvent = CloneTimelineEvent(timelineEvent);
            storedEvent.Id = string.IsNullOrWhiteSpace(timelineEvent.Id) ? "created-event" : timelineEvent.Id;
            Events.Add(storedEvent);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default)
        {
            UpdatedEvents.Add(CloneTimelineEvent(timelineEvent));
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            DeletedIds.Add(id);
            Events.RemoveAll(timelineEvent => string.Equals(timelineEvent.Id, id, StringComparison.Ordinal));
            return Task.CompletedTask;
        }

        public Task SeedAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        private static TimelineEvent CloneTimelineEvent(TimelineEvent timelineEvent)
        {
            return new TimelineEvent
            {
                Id = timelineEvent.Id,
                Title = timelineEvent.Title,
                ShortDescription = timelineEvent.ShortDescription,
                ImageUrl = timelineEvent.ImageUrl,
                EventDateUtc = timelineEvent.EventDateUtc,
                EventType = timelineEvent.EventType
            };
        }
    }
}
