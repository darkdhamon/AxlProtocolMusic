using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class TimelineEventServiceTests
{
    [Test]
    public async Task GetAllAsync_ReturnsEventsSortedByDescendingDate()
    {
        var repository = new InMemoryTimelineEventRepository(
        [
            CreateTimelineEvent("oldest", new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero)),
            CreateTimelineEvent("newest", new DateTimeOffset(2025, 4, 25, 0, 0, 0, TimeSpan.Zero)),
            CreateTimelineEvent("middle", new DateTimeOffset(2024, 10, 31, 0, 0, 0, TimeSpan.Zero))
        ]);

        var service = new TimelineEventService(repository);

        var result = await service.GetAllAsync();

        Assert.That(result.Select(item => item.Id), Is.EqualTo(new[] { "newest", "middle", "oldest" }));
    }

    [Test]
    public async Task CreateAsync_WhenIdIsBlank_GeneratesIdAndNormalizesFields()
    {
        var repository = new InMemoryTimelineEventRepository([]);
        var service = new TimelineEventService(repository);

        await service.CreateAsync(new TimelineEvent
        {
            Title = "  New Event  ",
            ShortDescription = "  Short description  ",
            ImageUrl = "  /images/timeline/new-event.png  ",
            EventDateUtc = new DateTimeOffset(2026, 3, 21, 14, 30, 0, TimeSpan.FromHours(-5)),
            EventType = TimelineEventType.Release
        });

        Assert.That(repository.CreatedDocuments, Has.Count.EqualTo(1));

        var created = repository.CreatedDocuments.Single();
        Assert.That(created.Id, Does.StartWith("timeline-event-"));
        Assert.That(created.Title, Is.EqualTo("New Event"));
        Assert.That(created.ShortDescription, Is.EqualTo("Short description"));
        Assert.That(created.ImageUrl, Is.EqualTo("/images/timeline/new-event.png"));
        Assert.That(created.EventDateUtc, Is.EqualTo(new DateTimeOffset(2026, 3, 21, 0, 0, 0, TimeSpan.Zero)));
        Assert.That(created.EventType, Is.EqualTo(TimelineEventType.Release));
    }

    [Test]
    public void CreateAsync_WhenTitleIsBlank_ThrowsInvalidOperationException()
    {
        var service = new TimelineEventService(new InMemoryTimelineEventRepository([]));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CreateAsync(new TimelineEvent
        {
            Title = "   ",
            EventDateUtc = new DateTimeOffset(2026, 3, 21, 0, 0, 0, TimeSpan.Zero)
        }));

        Assert.That(exception!.Message, Is.EqualTo("Title is required."));
    }

    [Test]
    public void CreateAsync_WhenEventDateIsDefault_ThrowsInvalidOperationException()
    {
        var service = new TimelineEventService(new InMemoryTimelineEventRepository([]));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CreateAsync(new TimelineEvent
        {
            Title = "Event without date"
        }));

        Assert.That(exception!.Message, Is.EqualTo("Event date is required."));
    }

    [Test]
    public async Task UpdateAsync_WhenEventExists_NormalizesAndPersistsChanges()
    {
        var existing = CreateTimelineEvent("timeline-event-1", new DateTimeOffset(2024, 10, 31, 0, 0, 0, TimeSpan.Zero));
        var repository = new InMemoryTimelineEventRepository([existing]);
        var service = new TimelineEventService(repository);

        await service.UpdateAsync(new TimelineEvent
        {
            Id = "timeline-event-1",
            Title = "  Updated Event  ",
            ShortDescription = "  Updated description  ",
            ImageUrl = "  /images/timeline/updated.png  ",
            EventDateUtc = new DateTimeOffset(2026, 3, 22, 18, 45, 0, TimeSpan.FromHours(2)),
            EventType = TimelineEventType.Platform
        });

        Assert.That(repository.UpdatedDocuments, Has.Count.EqualTo(1));

        var updated = repository.Documents.Single();
        Assert.That(updated.Title, Is.EqualTo("Updated Event"));
        Assert.That(updated.ShortDescription, Is.EqualTo("Updated description"));
        Assert.That(updated.ImageUrl, Is.EqualTo("/images/timeline/updated.png"));
        Assert.That(updated.EventDateUtc, Is.EqualTo(new DateTimeOffset(2026, 3, 22, 0, 0, 0, TimeSpan.Zero)));
        Assert.That(updated.EventType, Is.EqualTo(TimelineEventType.Platform));
    }

    [Test]
    public void UpdateAsync_WhenIdIsBlank_ThrowsInvalidOperationException()
    {
        var service = new TimelineEventService(new InMemoryTimelineEventRepository([]));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.UpdateAsync(new TimelineEvent
        {
            Title = "Event",
            EventDateUtc = new DateTimeOffset(2026, 3, 21, 0, 0, 0, TimeSpan.Zero)
        }));

        Assert.That(exception!.Message, Is.EqualTo("A timeline event id is required."));
    }

    [Test]
    public void UpdateAsync_WhenEventDoesNotExist_ThrowsInvalidOperationException()
    {
        var service = new TimelineEventService(new InMemoryTimelineEventRepository([]));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.UpdateAsync(new TimelineEvent
        {
            Id = "missing",
            Title = "Event",
            EventDateUtc = new DateTimeOffset(2026, 3, 21, 0, 0, 0, TimeSpan.Zero)
        }));

        Assert.That(exception!.Message, Is.EqualTo("The timeline event could not be found."));
    }

    [Test]
    public async Task DeleteAsync_WhenEventExists_DeletesIt()
    {
        var repository = new InMemoryTimelineEventRepository(
        [
            CreateTimelineEvent("delete-me", new DateTimeOffset(2025, 4, 25, 0, 0, 0, TimeSpan.Zero)),
            CreateTimelineEvent("keep-me", new DateTimeOffset(2024, 10, 31, 0, 0, 0, TimeSpan.Zero))
        ]);

        var service = new TimelineEventService(repository);

        await service.DeleteAsync("delete-me");

        Assert.That(repository.Documents.Select(item => item.Id), Is.EqualTo(new[] { "keep-me" }));
    }

    [Test]
    public void DeleteAsync_WhenIdIsBlank_ThrowsInvalidOperationException()
    {
        var service = new TimelineEventService(new InMemoryTimelineEventRepository([]));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.DeleteAsync(" "));

        Assert.That(exception!.Message, Is.EqualTo("A timeline event id is required."));
    }

    [Test]
    public void DeleteAsync_WhenEventDoesNotExist_ThrowsInvalidOperationException()
    {
        var service = new TimelineEventService(new InMemoryTimelineEventRepository([]));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.DeleteAsync("missing"));

        Assert.That(exception!.Message, Is.EqualTo("The timeline event could not be found."));
    }

    [Test]
    public async Task SeedAsync_WhenRepositoryIsEmpty_CreatesSeedEvents()
    {
        var repository = new InMemoryTimelineEventRepository([]);
        var service = new TimelineEventService(repository);

        await service.SeedAsync();

        Assert.That(repository.CreatedDocuments, Has.Count.EqualTo(7));
        Assert.That(repository.Documents.Select(item => item.Id), Does.Contain("timeline-event-october-31-2024-first-release"));
        Assert.That(repository.Documents.Select(item => item.Id), Does.Contain("timeline-event-april-25-2025-attempted-murder"));
    }

    [Test]
    public async Task SeedAsync_WhenRepositoryAlreadyHasEvents_DoesNothing()
    {
        var repository = new InMemoryTimelineEventRepository(
        [
            CreateTimelineEvent("existing", new DateTimeOffset(2026, 3, 21, 0, 0, 0, TimeSpan.Zero))
        ]);

        var service = new TimelineEventService(repository);

        await service.SeedAsync();

        Assert.That(repository.CreatedDocuments, Is.Empty);
        Assert.That(repository.Documents, Has.Count.EqualTo(1));
    }

    private static TimelineEvent CreateTimelineEvent(string id, DateTimeOffset eventDateUtc)
    {
        return new TimelineEvent
        {
            Id = id,
            Title = $"{id} title",
            ShortDescription = $"{id} description",
            ImageUrl = $"/images/{id}.png",
            EventDateUtc = eventDateUtc,
            EventType = TimelineEventType.Milestone
        };
    }

    private sealed class InMemoryTimelineEventRepository : IRepository<TimelineEvent>
    {
        public InMemoryTimelineEventRepository(IEnumerable<TimelineEvent> documents)
        {
            Documents = documents.ToList();
        }

        public List<TimelineEvent> Documents { get; }

        public List<TimelineEvent> CreatedDocuments { get; } = [];

        public List<TimelineEvent> UpdatedDocuments { get; } = [];

        public Task<IReadOnlyList<TimelineEvent>> FindAsync(Expression<Func<TimelineEvent, bool>> filter, CancellationToken cancellationToken = default)
        {
            var predicate = filter.Compile();
            return Task.FromResult<IReadOnlyList<TimelineEvent>>(Documents.Where(predicate).ToList());
        }

        public Task<IReadOnlyList<TimelineEvent>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TimelineEvent>>(Documents.ToList());

        public Task<TimelineEvent?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Documents.FirstOrDefault(document => string.Equals(document.Id, id, StringComparison.Ordinal)));

        public Task CreateAsync(TimelineEvent document, CancellationToken cancellationToken = default)
        {
            CreatedDocuments.Add(document);
            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TimelineEvent document, CancellationToken cancellationToken = default)
        {
            UpdatedDocuments.Add(document);

            var index = Documents.FindIndex(existing => string.Equals(existing.Id, document.Id, StringComparison.Ordinal));
            if (index >= 0)
            {
                Documents[index] = document;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            Documents.RemoveAll(document => string.Equals(document.Id, id, StringComparison.Ordinal));
            return Task.CompletedTask;
        }
    }
}
