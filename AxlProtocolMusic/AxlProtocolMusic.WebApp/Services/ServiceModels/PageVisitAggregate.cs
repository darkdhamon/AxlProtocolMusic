namespace AxlProtocolMusic.WebApp.Services.ServiceModels;

public sealed class PageVisitAggregate
{
    public string PagePath { get; init; } = string.Empty;

    public string PageTitle { get; init; } = string.Empty;

    public int VisitCount { get; init; }

    public double AverageDurationSeconds { get; init; }
}
