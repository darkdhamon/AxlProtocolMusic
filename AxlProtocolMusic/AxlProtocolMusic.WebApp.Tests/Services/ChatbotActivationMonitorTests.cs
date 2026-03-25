using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Microsoft.Extensions.Logging;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class ChatbotActivationMonitorTests
{
    [Test]
    public async Task Subscribe_IncrementsCount_AndDisposeRemovesSubscriber()
    {
        var logger = new FakeLogger<ChatbotActivationMonitor>();
        var monitor = new ChatbotActivationMonitor(logger);

        var subscription = monitor.Subscribe(_ => ValueTask.CompletedTask);
        Assert.That(monitor.ActiveSubscriberCount, Is.EqualTo(1));

        subscription.Dispose();

        Assert.That(monitor.ActiveSubscriberCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PublishAsync_NotifiesAllSubscribers()
    {
        var logger = new FakeLogger<ChatbotActivationMonitor>();
        var monitor = new ChatbotActivationMonitor(logger);
        var receivedStates = new List<(string Name, ChatbotActivationState State)>();

        using var first = monitor.Subscribe(state =>
        {
            receivedStates.Add(("first", state));
            return ValueTask.CompletedTask;
        });

        using var second = monitor.Subscribe(state =>
        {
            receivedStates.Add(("second", state));
            return ValueTask.CompletedTask;
        });

        var state = new ChatbotActivationState
        {
            IsDisabled = true,
            IsManuallyDisabled = false,
            DisabledReason = "Threshold reached."
        };

        await monitor.PublishAsync(state);

        Assert.That(receivedStates, Has.Count.EqualTo(2));
        Assert.That(receivedStates.Select(item => item.Name), Is.EquivalentTo(["first", "second"]));
        Assert.That(receivedStates.All(item => ReferenceEquals(item.State, state)), Is.True);
        Assert.That(logger.WarningMessages, Is.Empty);
    }

    [Test]
    public async Task PublishAsync_WhenSameStateInstanceIsRepublished_SkipsNotification()
    {
        var logger = new FakeLogger<ChatbotActivationMonitor>();
        var monitor = new ChatbotActivationMonitor(logger);
        var publishCount = 0;
        var state = new ChatbotActivationState
        {
            IsDisabled = true
        };

        using var subscription = monitor.Subscribe(_ =>
        {
            publishCount++;
            return ValueTask.CompletedTask;
        });

        await monitor.PublishAsync(state);
        await monitor.PublishAsync(state);

        Assert.That(publishCount, Is.EqualTo(1));
    }

    [Test]
    public async Task PublishAsync_WhenSubscriberThrows_LogsWarningAndContinues()
    {
        var logger = new FakeLogger<ChatbotActivationMonitor>();
        var monitor = new ChatbotActivationMonitor(logger);
        var successfulNotifications = 0;

        using var first = monitor.Subscribe(_ => throw new InvalidOperationException("Boom"));
        using var second = monitor.Subscribe(_ =>
        {
            successfulNotifications++;
            return ValueTask.CompletedTask;
        });

        await monitor.PublishAsync(new ChatbotActivationState
        {
            DisabledReason = "Manual override"
        });

        Assert.That(successfulNotifications, Is.EqualTo(1));
        Assert.That(logger.WarningMessages, Has.Count.EqualTo(1));
        Assert.That(logger.WarningMessages.Single(), Does.Contain("A chatbot activation subscriber failed while processing an update."));
    }

    [Test]
    public void PublishAsync_WhenCancellationIsRequested_PropagatesCancellation()
    {
        var logger = new FakeLogger<ChatbotActivationMonitor>();
        var monitor = new ChatbotActivationMonitor(logger);
        using var cancellationTokenSource = new CancellationTokenSource();
        var subscriberCalled = false;

        using var subscription = monitor.Subscribe(_ =>
        {
            subscriberCalled = true;
            return ValueTask.CompletedTask;
        });

        cancellationTokenSource.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await monitor.PublishAsync(new ChatbotActivationState(), cancellationTokenSource.Token));
        Assert.That(subscriberCalled, Is.False);
    }

    [Test]
    public void Subscribe_WhenListenerIsNull_Throws()
    {
        var logger = new FakeLogger<ChatbotActivationMonitor>();
        var monitor = new ChatbotActivationMonitor(logger);

        Assert.Throws<ArgumentNullException>(() => monitor.Subscribe(null!));
    }

    [Test]
    public void PublishAsync_WhenStateIsNull_Throws()
    {
        var logger = new FakeLogger<ChatbotActivationMonitor>();
        var monitor = new ChatbotActivationMonitor(logger);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await monitor.PublishAsync(null!));
    }

    private sealed class FakeLogger<T> : ILogger<T>
    {
        public List<string> WarningMessages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                WarningMessages.Add(formatter(state, exception));
            }
        }
    }
}
