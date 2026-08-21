using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using AxlProtocolMusic.WebApp.Services.Interfaces;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class ChatbotRequestRateLimiter : IChatbotRequestRateLimiter, IDisposable
{
    private static readonly TimeSpan PermitLifetime = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _pendingPermits = new();
    private readonly Timer _permitCleanupTimer;
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

    public ChatbotRequestRateLimiter()
    {
        _permitCleanupTimer = new Timer(
            _ => RemoveExpiredPermits(),
            null,
            PermitLifetime,
            PermitLifetime);
    }

    public bool TryAcquire(string partitionKey)
    {
        using var lease = _limiter.AttemptAcquire(partitionKey);
        return lease.IsAcquired;
    }

    public string? TryIssuePermit(string partitionKey)
    {
        if (!TryAcquire(partitionKey))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var permitToken = Guid.NewGuid().ToString("N");
        _pendingPermits[permitToken] = now.Add(PermitLifetime);
        return permitToken;
    }

    public bool TryConsumePermit(string permitToken)
    {
        return !string.IsNullOrWhiteSpace(permitToken)
            && _pendingPermits.TryRemove(permitToken, out var expiresAt)
            && expiresAt >= DateTimeOffset.UtcNow;
    }

    public void Dispose()
    {
        _permitCleanupTimer.Dispose();
        _limiter.Dispose();
    }

    private void RemoveExpiredPermits()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var pendingPermit in _pendingPermits)
        {
            if (pendingPermit.Value < now)
            {
                _pendingPermits.TryRemove(pendingPermit.Key, out _);
            }
        }
    }
}
