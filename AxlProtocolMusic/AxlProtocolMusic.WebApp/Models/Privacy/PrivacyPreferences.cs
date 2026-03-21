namespace AxlProtocolMusic.WebApp.Models.Privacy;

public sealed class PrivacyPreferences
{
    public bool AllowEssentialSiteMetrics { get; set; } = true;

    public bool ShareApproximateLocation { get; set; }

    public bool AllowEnhancedEngagementMetrics { get; set; }

    public bool AllowPersonalizationMetrics { get; set; }
}
