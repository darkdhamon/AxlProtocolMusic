using AxlProtocolMusic.WebApp.Models.Privacy;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IPrivacyPreferencesService
{
    Task<PrivacyPreferences> GetAsync(CancellationToken cancellationToken = default);

    Task<PrivacyPreferenceSaveResult> SaveAsync(PrivacyPreferences preferences, CancellationToken cancellationToken = default);

    Task<PrivacyPreferenceSaveResult> SyncApproximateLocationPreferenceAsync(PrivacyPreferences preferences, CancellationToken cancellationToken = default);
}
