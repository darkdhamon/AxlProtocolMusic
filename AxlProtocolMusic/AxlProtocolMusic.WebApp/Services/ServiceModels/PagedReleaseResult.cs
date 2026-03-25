namespace AxlProtocolMusic.WebApp.Services;

public sealed class PagedReleaseResult
{
    public IReadOnlyList<ReleaseListItemViewModel> Items { get; init; } = [];

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public string SearchTerm { get; init; } = string.Empty;
}
