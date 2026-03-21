using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Interfaces;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class TimelineEventService : ITimelineEventService
{
    private readonly IRepository<TimelineEvent> _timelineEventRepository;

    public TimelineEventService(IRepository<TimelineEvent> timelineEventRepository)
    {
        _timelineEventRepository = timelineEventRepository;
    }

    public async Task<IReadOnlyList<TimelineEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return (await _timelineEventRepository.GetAllAsync(cancellationToken))
            .OrderByDescending(item => item.EventDateUtc)
            .ToList();
    }

    public async Task CreateAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timelineEvent);

        var normalized = Normalize(timelineEvent);
        normalized.Id = string.IsNullOrWhiteSpace(normalized.Id)
            ? $"timeline-event-{Guid.NewGuid():N}"
            : normalized.Id;

        await _timelineEventRepository.CreateAsync(normalized, cancellationToken);
    }

    public async Task UpdateAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timelineEvent);

        if (string.IsNullOrWhiteSpace(timelineEvent.Id))
        {
            throw new InvalidOperationException("A timeline event id is required.");
        }

        var existingEvent = await _timelineEventRepository.GetByIdAsync(timelineEvent.Id, cancellationToken);
        if (existingEvent is null)
        {
            throw new InvalidOperationException("The timeline event could not be found.");
        }

        var normalized = Normalize(timelineEvent);
        await _timelineEventRepository.UpdateAsync(normalized, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("A timeline event id is required.");
        }

        var existingEvent = await _timelineEventRepository.GetByIdAsync(id, cancellationToken);
        if (existingEvent is null)
        {
            throw new InvalidOperationException("The timeline event could not be found.");
        }

        await _timelineEventRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingEvents = await _timelineEventRepository.GetAllAsync(cancellationToken);
        if (existingEvents.Count > 0)
        {
            return;
        }

        foreach (var timelineEvent in GetSeedTimelineEvents())
        {
            await _timelineEventRepository.CreateAsync(timelineEvent, cancellationToken);
        }
    }

    private static IReadOnlyList<TimelineEvent> GetSeedTimelineEvents()
    {
        return
        [
            new TimelineEvent
            {
                Id = "timeline-event-april-2024-ai-work-research",
                Title = "Bronze Harold Brown is assigned to research AI at work",
                EventDateUtc = new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero),
                EventType = TimelineEventType.Research,
                ShortDescription = "An early work assignment opens the door to deeper interest in AI and starts a chain of exploration that later feeds into music."
            },
            new TimelineEvent
            {
                Id = "timeline-event-june-2024-songgenerator-interest",
                Title = "Bronze Harold Brown becomes interested in AI-generated music and starts using SongGenerator.IO for fun",
                EventDateUtc = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero),
                EventType = TimelineEventType.Creative,
                ShortDescription = "Experimentation with SongGenerator.IO turns curiosity into hands-on creative play and makes AI music feel personal instead of theoretical."
            },
            new TimelineEvent
            {
                Id = "timeline-event-september-2024-release-research",
                Title = "Bronze Harold Brown begins researching how to release music",
                EventDateUtc = new DateTimeOffset(2024, 9, 1, 0, 0, 0, TimeSpan.Zero),
                EventType = TimelineEventType.Planning,
                ShortDescription = "The idea of putting music out into the world becomes serious enough to trigger practical research into distribution and release strategy."
            },
            new TimelineEvent
            {
                Id = "timeline-event-october-2024-artist-name",
                Title = "Bronze Harold Brown comes up with the artist name Axl Protocol",
                EventDateUtc = new DateTimeOffset(2024, 10, 1, 0, 0, 0, TimeSpan.Zero),
                EventType = TimelineEventType.Identity,
                ShortDescription = "Axl reflects a long-standing nickname, while Protocol points to the role AI plays in how the music is created."
            },
            new TimelineEvent
            {
                Id = "timeline-event-october-31-2024-first-release",
                Title = "Bronze Harold Brown releases his first music",
                EventDateUtc = new DateTimeOffset(2024, 10, 31, 0, 0, 0, TimeSpan.Zero),
                EventType = TimelineEventType.Release,
                ShortDescription = "The project crosses from concept into public reality with its first official music release."
            },
            new TimelineEvent
            {
                Id = "timeline-event-november-24-2024-suno-shift",
                Title = "Axl Protocol shifts from SongGenerator.IO to Suno AI",
                EventDateUtc = new DateTimeOffset(2024, 11, 24, 0, 0, 0, TimeSpan.Zero),
                EventType = TimelineEventType.Platform,
                ShortDescription = "The creative workflow changes in a meaningful way as the project moves from SongGenerator.IO to Suno AI."
            },
            new TimelineEvent
            {
                Id = "timeline-event-april-25-2025-attempted-murder",
                Title = "Bronze Harold Brown survives attempted murder, leading to the creation of \"My Tragic Love Story\"",
                EventDateUtc = new DateTimeOffset(2025, 4, 25, 0, 0, 0, TimeSpan.Zero),
                EventType = TimelineEventType.LifeEvent,
                ShortDescription = "A traumatic life event becomes the catalyst for the Axl Protocol album \"My Tragic Love Story.\""
            }
        ];
    }

    private static TimelineEvent Normalize(TimelineEvent timelineEvent)
    {
        if (string.IsNullOrWhiteSpace(timelineEvent.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (timelineEvent.EventDateUtc == default)
        {
            throw new InvalidOperationException("Event date is required.");
        }

        return new TimelineEvent
        {
            Id = timelineEvent.Id,
            Title = timelineEvent.Title.Trim(),
            ShortDescription = timelineEvent.ShortDescription?.Trim() ?? string.Empty,
            ImageUrl = timelineEvent.ImageUrl?.Trim() ?? string.Empty,
            EventDateUtc = new DateTimeOffset(
                DateTime.SpecifyKind(timelineEvent.EventDateUtc.UtcDateTime.Date, DateTimeKind.Utc)),
            EventType = timelineEvent.EventType
        };
    }
}
