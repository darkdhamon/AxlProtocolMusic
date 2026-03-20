namespace AxlProtocolMusic.WebApp.Services;

public sealed class ReleaseDetailsViewModel
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public string CoverImageUrl { get; init; } = string.Empty;

    public DateTimeOffset ReleaseDateUtc { get; init; }

    public bool IsPublished { get; init; }
}
