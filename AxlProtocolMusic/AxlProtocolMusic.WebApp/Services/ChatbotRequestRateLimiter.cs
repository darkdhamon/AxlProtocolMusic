using System.Threading.RateLimiting;
using AxlProtocolMusic.WebApp.Services.Interfaces;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class ChatbotRequestRateLimiter : IChatbotRequestRateLimiter, IDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter =
        PartitionedRateLimiter.Create<string, string>(partitionKey =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey,
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    AutoReplenishment = true,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

    public bool TryAcquire(string partitionKey)
    {
        using var lease = _limiter.AttemptAcquire(partitionKey);
        return lease.IsAcquired;
    }

    public void Dispose()
    {
        _limiter.Dispose();
    }
}
