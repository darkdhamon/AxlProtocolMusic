using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Models.Content;

public sealed class Release : IEntity
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string CoverImageUrl { get; set; } = string.Empty;

    public string Story { get; set; } = string.Empty;

    public string Lyrics { get; set; } = string.Empty;

    public List<ReleaseCredit> Credits { get; set; } = [];

    public List<ReleaseTrack> Tracks { get; set; } = [];

    public List<ReleaseLink> Links { get; set; } = [];

    public string ReleaseTypeOverride { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = [];

    public DateTimeOffset ReleaseDateUtc { get; set; }

    public bool IsPublished { get; set; }
}
