using AxlProtocolMusic.WebApp.Models.Content;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface ITimelineEventService
{
    Task<IReadOnlyList<TimelineEvent>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default);

    Task UpdateAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task SeedAsync(CancellationToken cancellationToken = default);
}
