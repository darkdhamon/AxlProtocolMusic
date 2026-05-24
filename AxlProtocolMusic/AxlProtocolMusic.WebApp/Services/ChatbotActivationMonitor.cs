using System.Collections.Concurrent;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class ChatbotActivationMonitor : IChatbotActivationMonitor
{
    private readonly ConcurrentDictionary<Guid, Func<ChatbotActivationState, ValueTask>> _listeners = new();
    private readonly ILogger<ChatbotActivationMonitor> _logger;
    private readonly Lock _stateLock = new();

    private ChatbotActivationState? _currentState;

    public ChatbotActivationMonitor(ILogger<ChatbotActivationMonitor> logger)
    {
        _logger = logger;
    }

    public int ActiveSubscriberCount => _listeners.Count;

    public IDisposable Subscribe(Func<ChatbotActivationState, ValueTask> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        var id = Guid.NewGuid();
        _listeners[id] = listener;
        return new Subscription(_listeners, id);
    }

    public async ValueTask PublishAsync(ChatbotActivationState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        Func<ChatbotActivationState, ValueTask>[] listeners;

        lock (_stateLock)
        {
            if (_currentState == state)
            {
                return;
            }

            _currentState = state;
            listeners = _listeners.Values.ToArray();
        }

        foreach (var listener in listeners)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await listener(state);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "A chatbot activation subscriber failed while processing an update.");
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, Func<ChatbotActivationState, ValueTask>> _listeners;
        private readonly Guid _id;
        private bool _disposed;

        public Subscription(
            ConcurrentDictionary<Guid, Func<ChatbotActivationState, ValueTask>> listeners,
            Guid id)
        {
            _listeners = listeners;
            _id = id;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _listeners.TryRemove(_id, out _);
            _disposed = true;
        }
    }
}
