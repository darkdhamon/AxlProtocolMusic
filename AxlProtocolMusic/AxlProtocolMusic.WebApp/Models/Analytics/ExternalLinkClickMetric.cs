using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Models.Analytics;

public sealed class ExternalLinkClickMetric : IEntity
{
    public string Id { get; set; } = string.Empty;

    public string SourcePagePath { get; set; } = string.Empty;

    public string DestinationUrl { get; set; } = string.Empty;

    public string LinkLabel { get; set; } = string.Empty;

    public DateTimeOffset ClickedAtUtc { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string Region { get; set; } = "Unknown";

    public double? ApproximateLatitude { get; set; }

    public double? ApproximateLongitude { get; set; }
}
