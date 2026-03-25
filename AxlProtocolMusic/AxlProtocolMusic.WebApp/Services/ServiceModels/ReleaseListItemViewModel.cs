namespace AxlProtocolMusic.WebApp.Services.ServiceModels;

public sealed class ReleaseListItemViewModel
{
    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public string CoverImageUrl { get; init; } = string.Empty;

    public DateTimeOffset ReleaseDateUtc { get; init; }

    public bool IsPublished { get; init; }
}
