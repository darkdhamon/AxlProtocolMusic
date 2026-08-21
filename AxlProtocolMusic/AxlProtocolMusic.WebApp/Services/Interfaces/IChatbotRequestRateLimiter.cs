namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IChatbotRequestRateLimiter
{
    bool TryAcquire(string partitionKey);
}
