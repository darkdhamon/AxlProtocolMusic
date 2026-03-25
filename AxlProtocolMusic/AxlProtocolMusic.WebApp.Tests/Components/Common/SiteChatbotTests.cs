using AxlProtocolMusic.WebApp.Components.Common;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AxlProtocolMusic.WebApp.Tests.Components.Common;

[TestFixture]
public sealed class SiteChatbotTests
{
    [Test]
    public async Task SiteChatbot_ShowsPopupAndClosesWhenAdminDisablesWhileOpen()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var chatbotBudgetService = new FakeChatbotBudgetService
        {
            Summary = new ChatbotBudgetSummary
            {
                DisableThresholdUsd = 10m,
                TotalEstimatedCostUsd = 1.5m
            }
        };
        var activationMonitor = new FakeChatbotActivationMonitor();

        context.Services.AddSingleton<IChatbotBudgetService>(chatbotBudgetService);
        context.Services.AddSingleton<IChatbotActivationMonitor>(activationMonitor);
        context.Services.AddSingleton<ISiteChatbotService>(new FakeSiteChatbotService());
        context.Services.AddSingleton<IOptions<ChatbotSettings>>(Options.Create(new ChatbotSettings
        {
            Enabled = true
        }));

        var cut = context.Render<SiteChatbot>();

        cut.Find(".chatbot-launcher").Click();
        Assert.That(cut.Markup, Does.Contain("site-chatbot-panel"));

        chatbotBudgetService.Summary = new ChatbotBudgetSummary
        {
            DisableThresholdUsd = 10m,
            TotalEstimatedCostUsd = 1.5m,
            IsDisabled = true,
            IsManuallyDisabled = true,
            DisabledReason = "Chatbot manually disabled from the admin dashboard."
        };

        await activationMonitor.PublishAsync(new ChatbotActivationState
        {
            IsDisabled = true,
            IsManuallyDisabled = true,
            DisabledReason = chatbotBudgetService.Summary.DisabledReason
        });

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Admin has disabled the chatbot for all users."));
            Assert.That(cut.Markup, Does.Not.Contain("site-chatbot-panel"));
            Assert.That(cut.Markup, Does.Not.Contain("chatbot-launcher"));
        });
    }

    private sealed class FakeChatbotBudgetService : IChatbotBudgetService
    {
        public ChatbotBudgetSummary Summary { get; set; } = new();

        public Task<ChatbotBudgetSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);

        public Task<ChatbotBudgetSummary> RecordUsageAsync(string model, long inputTokens, long outputTokens, long cachedInputTokens, CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);

        public Task DisableForQuotaErrorAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordFailureAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ResetAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ChatbotBudgetSummary> SetManualDisabledAsync(bool isDisabled, CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);
    }

    private sealed class FakeChatbotActivationMonitor : IChatbotActivationMonitor
    {
        private readonly List<Func<ChatbotActivationState, ValueTask>> _listeners = [];

        public int ActiveSubscriberCount => _listeners.Count;

        public IDisposable Subscribe(Func<ChatbotActivationState, ValueTask> listener)
        {
            _listeners.Add(listener);
            return new Subscription(_listeners, listener);
        }

        public async ValueTask PublishAsync(ChatbotActivationState state, CancellationToken cancellationToken = default)
        {
            foreach (var listener in _listeners.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await listener(state);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly List<Func<ChatbotActivationState, ValueTask>> _listeners;
            private readonly Func<ChatbotActivationState, ValueTask> _listener;

            public Subscription(
                List<Func<ChatbotActivationState, ValueTask>> listeners,
                Func<ChatbotActivationState, ValueTask> listener)
            {
                _listeners = listeners;
                _listener = listener;
            }

            public void Dispose()
            {
                _listeners.Remove(_listener);
            }
        }
    }

    private sealed class FakeSiteChatbotService : ISiteChatbotService
    {
        public Task<ChatbotMessageResponse> GenerateReplyAsync(
            string message,
            IReadOnlyList<ChatbotConversationMessage>? history = null,
            ChatbotPageContext? currentPage = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatbotMessageResponse
            {
                IsEnabled = true,
                IsConfigured = true,
                Message = "Test response"
            });
    }
}
