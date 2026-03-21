namespace AxlProtocolMusic.WebApp.Models.Analytics;

public sealed class PageVisitRequest
{
    public string PagePath { get; set; } = string.Empty;

    public string PageTitle { get; set; } = string.Empty;

    public double DurationSeconds { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string ReferrerPath { get; set; } = string.Empty;

    public string ApproximateLocation { get; set; } = string.Empty;

    public double? ApproximateLatitude { get; set; }

    public double? ApproximateLongitude { get; set; }
}
