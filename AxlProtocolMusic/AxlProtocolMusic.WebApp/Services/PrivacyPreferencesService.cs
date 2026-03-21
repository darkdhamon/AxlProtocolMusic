using AxlProtocolMusic.WebApp.Models.Privacy;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.JSInterop;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class PrivacyPreferencesService : IPrivacyPreferencesService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public PrivacyPreferencesService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<PrivacyPreferences> GetAsync(CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<PrivacyPreferences>("getPreferences", cancellationToken);
    }

    public async Task<PrivacyPreferenceSaveResult> SaveAsync(PrivacyPreferences preferences, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<PrivacyPreferenceSaveResult>("savePreferences", cancellationToken, preferences);
    }

    public async Task<PrivacyPreferenceSaveResult> SyncApproximateLocationPreferenceAsync(PrivacyPreferences preferences, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<PrivacyPreferenceSaveResult>("syncApproximateLocationPreference", cancellationToken, preferences);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Ignore circuit disconnects during disposal.
            }
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/privacyPreferences.js");
        return _module;
    }
}
