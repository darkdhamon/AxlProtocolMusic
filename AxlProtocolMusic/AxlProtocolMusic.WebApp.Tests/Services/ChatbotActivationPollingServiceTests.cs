using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class ChatbotActivationPollingServiceTests
{
    [Test]
    public async Task ExecuteAsync_WhenSubscribersExist_PublishesLatestBudgetState()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var budgetService = new FakeChatbotBudgetService
        {
            Summary = new ChatbotBudgetSummary
            {
                IsDisabled = true,
                IsManuallyDisabled = true,
                DisabledReason = "Manually disabled.",
                LastUpdatedUtc = DateTimeOffset.UtcNow
            },
            OnGetSummary = () => cancellationTokenSource.Cancel()
        };
        var scopeFactory = new FakeServiceScopeFactory(budgetService);
        var activationMonitor = new FakeChatbotActivationMonitor
        {
            ActiveSubscriberCountValue = 2
        };
        var logger = new FakeLogger<ChatbotActivationPollingService>();
        var service = new ChatbotActivationPollingService(scopeFactory, activationMonitor, logger);

        try
        {
            await ExecuteAsync(service, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // The harness cancels after the first successful poll to stop the background loop.
        }

        Assert.That(scopeFactory.CreateScopeCallCount, Is.EqualTo(1));
        Assert.That(budgetService.GetSummaryCallCount, Is.EqualTo(1));
        Assert.That(activationMonitor.PublishedStates, Has.Count.EqualTo(1));

        var state = activationMonitor.PublishedStates.Single();
        Assert.That(state.IsDisabled, Is.True);
        Assert.That(state.IsManuallyDisabled, Is.True);
        Assert.That(state.DisabledReason, Is.EqualTo("Manually disabled."));
        Assert.That(state.LastUpdatedUtc, Is.EqualTo(budgetService.Summary.LastUpdatedUtc));
        Assert.That(logger.ErrorMessages, Is.Empty);
    }

    [Test]
    public async Task ExecuteAsync_WhenNoSubscribersExist_DoesNotCreateScopeOrPublish()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var budgetService = new FakeChatbotBudgetService();
        var scopeFactory = new FakeServiceScopeFactory(budgetService);
        var activationMonitor = new FakeChatbotActivationMonitor
        {
            ActiveSubscriberCountValue = 0
        };
        var logger = new FakeLogger<ChatbotActivationPollingService>();
        var service = new ChatbotActivationPollingService(scopeFactory, activationMonitor, logger);

        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        try
        {
            await ExecuteAsync(service, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // WaitForNextTickAsync can observe the cancellation after the no-subscriber check.
        }

        Assert.That(scopeFactory.CreateScopeCallCount, Is.EqualTo(0));
        Assert.That(budgetService.GetSummaryCallCount, Is.EqualTo(0));
        Assert.That(activationMonitor.PublishedStates, Is.Empty);
        Assert.That(logger.ErrorMessages, Is.Empty);
    }

    [Test]
    public async Task ExecuteAsync_WhenPollingThrows_LogsErrorAndContinuesUntilCancelled()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var budgetService = new FakeChatbotBudgetService
        {
            ExceptionToThrow = new InvalidOperationException("Boom")
        };
        var scopeFactory = new FakeServiceScopeFactory(budgetService);
        var activationMonitor = new FakeChatbotActivationMonitor
        {
            ActiveSubscriberCountValue = 1
        };
        var logger = new FakeLogger<ChatbotActivationPollingService>();
        var service = new ChatbotActivationPollingService(scopeFactory, activationMonitor, logger);

        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        try
        {
            await ExecuteAsync(service, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // WaitForNextTickAsync can observe the cancellation outside the inner try/catch.
        }

        Assert.That(scopeFactory.CreateScopeCallCount, Is.EqualTo(1));
        Assert.That(budgetService.GetSummaryCallCount, Is.EqualTo(1));
        Assert.That(activationMonitor.PublishedStates, Is.Empty);
        Assert.That(logger.ErrorMessages, Has.Count.EqualTo(1));
        Assert.That(logger.ErrorMessages.Single(), Does.Contain("Failed to poll the chatbot activation state."));
    }

    private static Task ExecuteAsync(ChatbotActivationPollingService service, CancellationToken cancellationToken)
    {
        var method = typeof(ChatbotActivationPollingService).GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);

        return (Task)method!.Invoke(service, [cancellationToken])!;
    }

    private sealed class FakeChatbotBudgetService : IChatbotBudgetService
    {
        public ChatbotBudgetSummary Summary { get; set; } = new();

        public int GetSummaryCallCount { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public Action? OnGetSummary { get; set; }

        public Task<ChatbotBudgetSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            GetSummaryCallCount++;
            OnGetSummary?.Invoke();

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(Summary);
        }

        public Task<ChatbotBudgetSummary> RecordUsageAsync(string model, long inputTokens, long outputTokens, long cachedInputTokens, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DisableForQuotaErrorAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task RecordFailureAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ResetAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ChatbotBudgetSummary> SetManualDisabledAsync(bool isDisabled, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeChatbotActivationMonitor : IChatbotActivationMonitor
    {
        public int ActiveSubscriberCountValue { get; set; }

        public List<ChatbotActivationState> PublishedStates { get; } = [];

        public int ActiveSubscriberCount => ActiveSubscriberCountValue;

        public IDisposable Subscribe(Func<ChatbotActivationState, ValueTask> listener)
            => new NullDisposable();

        public ValueTask PublishAsync(ChatbotActivationState state, CancellationToken cancellationToken = default)
        {
            PublishedStates.Add(state);
            return ValueTask.CompletedTask;
        }

        private sealed class NullDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class FakeServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IChatbotBudgetService _chatbotBudgetService;

        public FakeServiceScopeFactory(IChatbotBudgetService chatbotBudgetService)
        {
            _chatbotBudgetService = chatbotBudgetService;
        }

        public int CreateScopeCallCount { get; private set; }

        public IServiceScope CreateScope()
        {
            CreateScopeCallCount++;
            return new FakeServiceScope(_chatbotBudgetService);
        }
    }

    private sealed class FakeServiceScope : IServiceScope
    {
        public FakeServiceScope(IChatbotBudgetService chatbotBudgetService)
        {
            ServiceProvider = new FakeServiceProvider(chatbotBudgetService);
        }

        public IServiceProvider ServiceProvider { get; }

        public void Dispose()
        {
        }
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        private readonly IChatbotBudgetService _chatbotBudgetService;

        public FakeServiceProvider(IChatbotBudgetService chatbotBudgetService)
        {
            _chatbotBudgetService = chatbotBudgetService;
        }

        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(IChatbotBudgetService)
                ? _chatbotBudgetService
                : null;
        }
    }

    private sealed class FakeLogger<T> : ILogger<T>
    {
        public List<string> ErrorMessages { get; } = [];

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
            if (logLevel == LogLevel.Error)
            {
                ErrorMessages.Add(formatter(state, exception));
            }
        }
    }
}
