using System.Net;
using System.Net.Http;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class SiteChatbotServiceTests
{
    [Test]
    public async Task GenerateReplyAsync_WhenChatbotIsDisabled_ReturnsDisabledMessageWithoutCallingDependencies()
    {
        var contextBuilder = new FakeContextBuilder();
        var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("HTTP should not be called."));
        var service = CreateService(
            handler,
            contextBuilder,
            chatbotSettings: new ChatbotSettings { Enabled = false },
            openAiSettings: new OpenAiChatSettings { ApiKey = "test-key" });

        var result = await service.GenerateReplyAsync("What is this site?");

        Assert.That(result.IsEnabled, Is.False);
        Assert.That(result.IsConfigured, Is.False);
        Assert.That(result.Message, Is.EqualTo("The site assistant is currently turned off."));
        Assert.That(contextBuilder.BuildCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenApiKeyIsMissing_ReturnsConfigurationMessageWithoutCallingOpenAi()
    {
        var contextBuilder = new FakeContextBuilder();
        var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("HTTP should not be called."));
        var service = CreateService(
            handler,
            contextBuilder,
            chatbotSettings: new ChatbotSettings { Enabled = true },
            openAiSettings: new OpenAiChatSettings { ApiKey = "" });

        var result = await service.GenerateReplyAsync("What is this site?");

        Assert.That(result.IsEnabled, Is.True);
        Assert.That(result.IsConfigured, Is.False);
        Assert.That(result.Message, Is.EqualTo("The site assistant is enabled, but the OpenAI API key has not been configured yet."));
        Assert.That(contextBuilder.BuildCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenOpenAiReturnsOutputText_UsesItAsTheReply()
    {
        var contextBuilder = new FakeContextBuilder();
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"output_text":"Use /releases to browse the latest catalog."}""")
        });

        var service = CreateService(
            handler,
            contextBuilder,
            chatbotSettings: new ChatbotSettings { Enabled = true },
            openAiSettings: new OpenAiChatSettings
            {
                ApiKey = "test-key",
                Model = "gpt-5-mini",
                BaseUrl = "https://api.openai.com/v1/"
            });

        var result = await service.GenerateReplyAsync(
            "Where should I start?",
            [new ChatbotConversationMessage { Role = "user", Content = "I am new here." }]);

        Assert.That(result.IsEnabled, Is.True);
        Assert.That(result.IsConfigured, Is.True);
        Assert.That(result.Message, Is.EqualTo("Use /releases to browse the latest catalog."));
        Assert.That(contextBuilder.BuildCallCount, Is.EqualTo(1));
    }

    private static SiteChatbotService CreateService(
        HttpMessageHandler handler,
        ISiteChatbotContextBuilder contextBuilder,
        ChatbotSettings chatbotSettings,
        OpenAiChatSettings openAiSettings)
    {
        return new SiteChatbotService(
            new HttpClient(handler),
            new FakeChatbotBudgetService(),
            contextBuilder,
            Options.Create(chatbotSettings),
            Options.Create(openAiSettings),
            NullLogger<SiteChatbotService>.Instance);
    }

    private sealed class FakeContextBuilder : ISiteChatbotContextBuilder
    {
        public int BuildCallCount { get; private set; }

        public Task<string> BuildAsync(CancellationToken cancellationToken = default)
        {
            BuildCallCount++;
            return Task.FromResult("Releases: /releases");
        }
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }

    private sealed class FakeChatbotBudgetService : IChatbotBudgetService
    {
        public Task DisableForQuotaErrorAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordFailureAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ChatbotBudgetSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatbotBudgetSummary());

        public Task<ChatbotBudgetSummary> RecordUsageAsync(string model, long inputTokens, long outputTokens, long cachedInputTokens, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatbotBudgetSummary());

        public Task ResetAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
