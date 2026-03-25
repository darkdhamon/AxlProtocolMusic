namespace AxlProtocolMusic.WebApp.Services.ServiceModels;

public sealed class AnalyticsDashboardSummary
{
    public IReadOnlyList<PageVisitAggregate> TopPages { get; init; } = [];

    public IReadOnlyList<RegionVisitAggregate> TopRegions { get; init; } = [];

    public IReadOnlyList<ExternalLinkClickAggregate> TopExternalLinks { get; init; } = [];

    public int TotalVisits { get; init; }

    public int UniqueVisitors { get; init; }

    public int RepeatVisitors { get; init; }

    public double AverageDurationSeconds { get; init; }
}
