using AxlProtocolMusic.WebApp.Models.Content;

namespace AxlProtocolMusic.WebApp.Services.ServiceModels;

public sealed class ReleaseDetailsViewModel
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public string CoverImageUrl { get; init; } = string.Empty;

    public string Story { get; init; } = string.Empty;

    public IReadOnlyList<ReleaseCredit> Credits { get; init; } = [];

    public IReadOnlyList<ReleaseTrack> Tracks { get; init; } = [];

    public IReadOnlyList<ReleaseLink> Links { get; init; } = [];

    public string ReleaseType { get; init; } = string.Empty;

    public string ReleaseTypeOverride { get; init; } = string.Empty;

    public IReadOnlyList<string> Tags { get; init; } = [];

    public DateTimeOffset ReleaseDateUtc { get; init; }

    public bool IsPublished { get; init; }
}
