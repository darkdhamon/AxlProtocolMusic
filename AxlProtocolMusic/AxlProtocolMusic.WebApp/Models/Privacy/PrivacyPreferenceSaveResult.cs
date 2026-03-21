namespace AxlProtocolMusic.WebApp.Models.Privacy;

public sealed class PrivacyPreferenceSaveResult
{
    public PrivacyPreferences Preferences { get; set; } = new();

    public bool LocationPermissionDenied { get; set; }
}
