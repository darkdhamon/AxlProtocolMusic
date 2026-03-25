namespace AxlProtocolMusic.WebApp.Services.ServiceModels;

public sealed class VisitorExternalLinkClickViewModel
{
    public string SourcePagePath { get; set; } = string.Empty;

    public string DestinationUrl { get; set; } = string.Empty;

    public string LinkLabel { get; set; } = string.Empty;

    public DateTimeOffset ClickedAtUtc { get; set; }

    public string Region { get; set; } = "Unknown";

    public double? ApproximateLatitude { get; set; }

    public double? ApproximateLongitude { get; set; }
}
