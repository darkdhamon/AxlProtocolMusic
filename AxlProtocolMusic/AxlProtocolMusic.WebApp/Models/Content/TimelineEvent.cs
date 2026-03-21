using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Models.Content;

public sealed class TimelineEvent : IEntity
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public DateTimeOffset EventDateUtc { get; set; }

    public TimelineEventType EventType { get; set; } = TimelineEventType.Milestone;
}
