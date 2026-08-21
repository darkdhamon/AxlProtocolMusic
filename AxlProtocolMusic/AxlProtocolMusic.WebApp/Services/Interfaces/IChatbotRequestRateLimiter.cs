namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IChatbotRequestRateLimiter
{
    bool TryAcquire(string partitionKey);

    string? TryIssuePermit(string partitionKey);

    bool TryConsumePermit(string permitToken);
}
