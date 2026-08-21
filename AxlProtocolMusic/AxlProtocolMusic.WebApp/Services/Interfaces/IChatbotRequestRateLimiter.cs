namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IChatbotRequestRateLimiter
{
    Task<string?> TryIssuePermitAsync(string deviceId, CancellationToken cancellationToken = default);

    Task<bool> TryConsumePermitAsync(string permitToken, CancellationToken cancellationToken = default);
}
