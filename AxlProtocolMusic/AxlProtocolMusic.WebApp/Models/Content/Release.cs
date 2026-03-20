using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Models.Content;

public sealed class Release : IEntity
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string CoverImageUrl { get; set; } = string.Empty;

    public DateTimeOffset ReleaseDateUtc { get; set; }

    public bool IsPublished { get; set; }
}
