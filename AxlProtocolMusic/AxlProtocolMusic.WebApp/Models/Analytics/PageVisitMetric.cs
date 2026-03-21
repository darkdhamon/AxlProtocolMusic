using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Models.Analytics;

public sealed class PageVisitMetric : IEntity
{
    public string Id { get; set; } = string.Empty;

    public string PagePath { get; set; } = string.Empty;

    public string PageTitle { get; set; } = string.Empty;

    public double DurationSeconds { get; set; }

    public DateTimeOffset VisitedAtUtc { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string Region { get; set; } = "Unknown";

    public double? ApproximateLatitude { get; set; }

    public double? ApproximateLongitude { get; set; }

    public string ReferrerPath { get; set; } = string.Empty;
}
