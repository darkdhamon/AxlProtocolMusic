namespace AxlProtocolMusic.WebApp.Services.ServiceModels;

public sealed class RegionVisitAggregate
{
    public string Region { get; init; } = string.Empty;

    public int VisitCount { get; init; }
}
