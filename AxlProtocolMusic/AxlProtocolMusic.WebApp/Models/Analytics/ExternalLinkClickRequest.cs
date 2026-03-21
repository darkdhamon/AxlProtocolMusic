namespace AxlProtocolMusic.WebApp.Models.Analytics;

public sealed class ExternalLinkClickRequest
{
    public string SourcePagePath { get; set; } = string.Empty;

    public string DestinationUrl { get; set; } = string.Empty;

    public string LinkLabel { get; set; } = string.Empty;

    public string ApproximateLocation { get; set; } = string.Empty;

    public double? ApproximateLatitude { get; set; }

    public double? ApproximateLongitude { get; set; }
}
