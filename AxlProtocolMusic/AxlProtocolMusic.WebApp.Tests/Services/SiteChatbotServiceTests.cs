using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
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
    public void GenerateReplyAsync_WhenMessageIsBlank_ThrowsInvalidOperationException()
    {
        var service = CreateService(new FakeHttpMessageHandler(_ => throw new AssertionException("HTTP should not be called.")));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.GenerateReplyAsync("   "));

        Assert.That(exception!.Message, Is.EqualTo("A message is required."));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenApiKeyIsMissing_ReturnsConfigurationMessageWithoutCallingOpenAi()
    {
        var contextBuilder = new FakeContextBuilder();
        var budgetService = new FakeChatbotBudgetService();
        var logService = new FakeChatbotConversationLogService();
        var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("HTTP should not be called."));
        var service = CreateService(
            handler,
            budgetService,
            logService,
            contextBuilder,
            openAiSettings: new OpenAiChatSettings { ApiKey = "" });

        var result = await service.GenerateReplyAsync("What is this site?");

        Assert.That(result.IsEnabled, Is.True);
        Assert.That(result.IsConfigured, Is.False);
        Assert.That(result.Message, Is.EqualTo("The site assistant is enabled, but the OpenAI API key has not been configured yet."));
        Assert.That(contextBuilder.BuildCallCount, Is.EqualTo(0));
        Assert.That(budgetService.GetSummaryCallCount, Is.EqualTo(0));
        Assert.That(logService.Records, Has.Count.EqualTo(1));
        Assert.That(logService.Records.Single().Outcome, Is.EqualTo("configuration-unavailable"));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenBudgetIsDisabledAndReasonIsPresent_ReturnsBudgetReason()
    {
        var budgetService = new FakeChatbotBudgetService
        {
            Summary = new ChatbotBudgetSummary
            {
                IsDisabled = true,
                DisabledReason = "Chatbot budget reached for today."
            }
        };

        var contextBuilder = new FakeContextBuilder();
        var logService = new FakeChatbotConversationLogService();
        var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("HTTP should not be called."));
        var service = CreateService(handler, budgetService, logService, contextBuilder);

        var result = await service.GenerateReplyAsync("Where can I find releases?");

        Assert.That(result.IsEnabled, Is.True);
        Assert.That(result.IsConfigured, Is.True);
        Assert.That(result.Message, Is.EqualTo("Chatbot budget reached for today."));
        Assert.That(budgetService.GetSummaryCallCount, Is.EqualTo(1));
        Assert.That(contextBuilder.BuildCallCount, Is.EqualTo(0));
        Assert.That(logService.Records.Single().Outcome, Is.EqualTo("disabled"));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenBudgetIsDisabledWithoutReason_ReturnsDefaultBudgetMessage()
    {
        var budgetService = new FakeChatbotBudgetService
        {
            Summary = new ChatbotBudgetSummary
            {
                IsDisabled = true,
                DisabledReason = " "
            }
        };

        var service = CreateService(
            new FakeHttpMessageHandler(_ => throw new AssertionException("HTTP should not be called.")),
            budgetService,
            new FakeChatbotConversationLogService(),
            new FakeContextBuilder());

        var result = await service.GenerateReplyAsync("Where can I find releases?");

        Assert.That(result.Message, Is.EqualTo("The site assistant is disabled until an admin resets the chatbot budget."));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenOpenAiReturnsOutputText_UsesItAsTheReplyAndRecordsUsage()
    {
        var contextBuilder = new FakeContextBuilder();
        var budgetService = new FakeChatbotBudgetService();
        var logService = new FakeChatbotConversationLogService();
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
              "output_text":"Use /releases to browse the latest catalog.",
              "usage":{
                "input_tokens":123,
                "output_tokens":45,
                "input_tokens_details":{"cached_tokens":6}
              }
            }
            """)
        });

        var service = CreateService(
            handler,
            budgetService,
            logService,
            contextBuilder,
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
        Assert.That(budgetService.UsageRecords, Has.Count.EqualTo(1));
        Assert.That(budgetService.UsageRecords.Single().Model, Is.EqualTo("gpt-5-mini"));
        Assert.That(budgetService.UsageRecords.Single().InputTokens, Is.EqualTo(123));
        Assert.That(budgetService.UsageRecords.Single().OutputTokens, Is.EqualTo(45));
        Assert.That(budgetService.UsageRecords.Single().CachedInputTokens, Is.EqualTo(6));
        Assert.That(logService.Records.Single().Outcome, Is.EqualTo("completed"));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenOutputTextIsMissing_CombinesOutputContentText()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
              "output":[
                {
                  "content":[
                    { "text":"  First line  " },
                    { "text":{"value":" Second line "} }
                  ]
                }
              ]
            }
            """)
        });

        var logService = new FakeChatbotConversationLogService();
        var service = CreateService(handler, logService: logService);

        var result = await service.GenerateReplyAsync("Summarize the page.");

        Assert.That(result.Message, Is.EqualTo("First line" + Environment.NewLine + "Second line"));
        Assert.That(logService.Records.Single().Outcome, Is.EqualTo("completed"));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenResponseHasNoReadableText_ReturnsFallbackMessageAndStillRecordsUsage()
    {
        var budgetService = new FakeChatbotBudgetService();
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
              "usage":{
                "input_tokens":10,
                "output_tokens":5,
                "input_tokens_details":{"cached_tokens":2}
              },
              "output":[{"content":[{"type":"other"}]}]
            }
            """)
        });

        var logService = new FakeChatbotConversationLogService();
        var service = CreateService(handler, budgetService: budgetService, logService: logService, contextBuilder: new FakeContextBuilder());

        var result = await service.GenerateReplyAsync("Tell me about this page.");

        Assert.That(result.Message, Is.EqualTo("The site assistant could not produce a readable reply right now."));
        Assert.That(budgetService.UsageRecords, Has.Count.EqualTo(1));
        Assert.That(logService.Records.Single().Outcome, Is.EqualTo("unreadable-response"));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenResponseIsSuccessful_BuildsExpectedRequestPayload()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var contextBuilder = new FakeContextBuilder
        {
            SiteContext = " Releases: /releases "
        };

        var longHistory = Enumerable.Range(1, 8)
            .Select(index => new ChatbotConversationMessage
            {
                Role = index % 2 == 0 ? "assistant" : "user",
                Content = $" message-{index} "
            })
            .ToList();

        longHistory.Insert(0, new ChatbotConversationMessage { Role = "system", Content = "ignore me" });
        longHistory.Insert(1, new ChatbotConversationMessage { Role = "user", Content = "   " });
        longHistory.Add(new ChatbotConversationMessage
        {
            Role = "assistant",
            Content = new string('x', 900)
        });

        var currentPage = new ChatbotPageContext
        {
            PagePath = "  /releases/demo  ",
            PageTitle = "  Demo Release  ",
            PageContent = "  " + new string('p', 3300) + "  "
        };

        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedRequest = request;
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"output_text":"ok"}""")
            };
        });

        var service = CreateService(
            handler,
            new FakeChatbotBudgetService(),
            new FakeChatbotConversationLogService(),
            contextBuilder,
            openAiSettings: new OpenAiChatSettings
            {
                ApiKey = "test-key",
                Model = " custom-model ",
                BaseUrl = "https://example.test/custom-base"
            });

        await service.GenerateReplyAsync("  Where do I go?  ", longHistory, currentPage);

        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(capturedRequest.RequestUri, Is.EqualTo(new Uri("https://example.test/custom-base/responses")));
        Assert.That(capturedRequest.Headers.Authorization, Is.EqualTo(new AuthenticationHeaderValue("Bearer", "test-key")));
        Assert.That(capturedRequest.Content!.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));

        using var document = JsonDocument.Parse(capturedBody!);
        var root = document.RootElement;

        Assert.That(root.GetProperty("model").GetString(), Is.EqualTo("custom-model"));
        Assert.That(root.GetProperty("max_output_tokens").GetInt32(), Is.EqualTo(900));
        Assert.That(root.GetProperty("reasoning").GetProperty("effort").GetString(), Is.EqualTo("low"));

        var instructions = root.GetProperty("instructions").GetString();
        Assert.That(instructions, Does.Contain("Current page context:"));
        Assert.That(instructions, Does.Contain("- Path: /releases/demo"));
        Assert.That(instructions, Does.Contain("- Title: Demo Release"));
        Assert.That(instructions, Does.Contain("Site context:"));
        Assert.That(instructions, Does.Contain(" Releases: /releases "));
        Assert.That(instructions, Does.Contain(new string('p', 3200)));
        Assert.That(instructions, Does.Not.Contain(new string('p', 3201)));

        var input = root.GetProperty("input").EnumerateArray().ToList();
        Assert.That(input, Has.Count.EqualTo(7));
        Assert.That(input.Last().GetProperty("role").GetString(), Is.EqualTo("user"));
        Assert.That(input.Last().GetProperty("content")[0].GetProperty("text").GetString(), Is.EqualTo("Where do I go?"));

        var historyEntries = input.Take(input.Count - 1).ToList();
        Assert.That(historyEntries.Select(item => item.GetProperty("role").GetString()).ToArray(), Is.EqualTo(new[] { "assistant", "user", "assistant", "user", "assistant", "assistant" }));
        Assert.That(historyEntries[0].GetProperty("content")[0].GetProperty("type").GetString(), Is.EqualTo("output_text"));
        Assert.That(historyEntries[1].GetProperty("content")[0].GetProperty("type").GetString(), Is.EqualTo("input_text"));
        Assert.That(historyEntries.Last().GetProperty("content")[0].GetProperty("text").GetString(), Has.Length.EqualTo(803));
        Assert.That(historyEntries.Last().GetProperty("content")[0].GetProperty("text").GetString(), Does.EndWith("..."));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenModelAndBaseUrlAreBlank_UsesDefaults()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var budgetService = new FakeChatbotBudgetService();

        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedRequest = request;
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"output_text":"ok","usage":{}}""")
            };
        });

        var service = CreateService(
            handler,
            budgetService,
            new FakeChatbotConversationLogService(),
            new FakeContextBuilder(),
            openAiSettings: new OpenAiChatSettings
            {
                ApiKey = "test-key",
                Model = " ",
                BaseUrl = " "
            });

        await service.GenerateReplyAsync("Where should I start?");

        Assert.That(capturedRequest!.RequestUri, Is.EqualTo(new Uri("https://api.openai.com/v1/responses")));

        using var document = JsonDocument.Parse(capturedBody!);
        Assert.That(document.RootElement.GetProperty("model").GetString(), Is.EqualTo("gpt-5-mini"));
        Assert.That(budgetService.UsageRecords.Single().Model, Is.EqualTo("gpt-5-mini"));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenQuotaErrorOccurs_DisablesBudgetAndReturnsFailureMessage()
    {
        var budgetService = new FakeChatbotBudgetService();
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("""
            {
              "error":{
                "message":"You exceeded your current quota.",
                "code":"insufficient_quota",
                "type":"insufficient_quota"
              }
            }
            """)
        });

        var logService = new FakeChatbotConversationLogService();
        var service = CreateService(handler, budgetService: budgetService, logService: logService, contextBuilder: new FakeContextBuilder());

        var result = await service.GenerateReplyAsync("Can you help?");

        Assert.That(result.Message, Is.EqualTo("The site assistant is temporarily unavailable: You exceeded your current quota."));
        Assert.That(budgetService.DisableForQuotaRecords, Has.Count.EqualTo(1));
        Assert.That(budgetService.FailureRecords, Is.Empty);
        Assert.That(budgetService.DisableForQuotaRecords.Single().Model, Is.EqualTo("gpt-5-mini"));
        Assert.That(budgetService.DisableForQuotaRecords.Single().ErrorCode, Is.EqualTo("insufficient_quota"));
        Assert.That(logService.Records.Single().Outcome, Is.EqualTo("failed"));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenNonQuotaErrorOccurs_RecordsFailureAndReturnsFallbackMessage()
    {
        var budgetService = new FakeChatbotBudgetService();
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("""
            {
              "error":{
                "message":"Gateway timeout from upstream.",
                "code":"server_error",
                "type":"server_error"
              }
            }
            """)
        });

        var logService = new FakeChatbotConversationLogService();
        var service = CreateService(handler, budgetService: budgetService, logService: logService, contextBuilder: new FakeContextBuilder());

        var result = await service.GenerateReplyAsync("Can you help?");

        Assert.That(result.Message, Is.EqualTo("The site assistant is temporarily unavailable: Gateway timeout from upstream."));
        Assert.That(budgetService.FailureRecords, Has.Count.EqualTo(1));
        Assert.That(budgetService.DisableForQuotaRecords, Is.Empty);
        Assert.That(budgetService.FailureRecords.Single().ErrorCode, Is.EqualTo("server_error"));
        Assert.That(logService.Records.Single().Outcome, Is.EqualTo("failed"));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenErrorPayloadIsUnreadable_ReturnsGenericFailureMessage()
    {
        var budgetService = new FakeChatbotBudgetService();
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("not-json")
        });

        var logService = new FakeChatbotConversationLogService();
        var service = CreateService(handler, budgetService: budgetService, logService: logService, contextBuilder: new FakeContextBuilder());

        var result = await service.GenerateReplyAsync("Can you help?");

        Assert.That(result.Message, Is.EqualTo("The site assistant is temporarily unavailable."));
        Assert.That(budgetService.FailureRecords, Has.Count.EqualTo(1));
        Assert.That(budgetService.FailureRecords.Single().ErrorMessage, Is.EqualTo(string.Empty));
        Assert.That(logService.Records.Single().Outcome, Is.EqualTo("failed"));
    }

    [Test]
    public async Task GenerateReplyAsync_WhenConversationLoggingFails_ReturnsReplyAnyway()
    {
        var budgetService = new FakeChatbotBudgetService();
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
              "output_text":"Use /news to follow updates.",
              "usage":{}
            }
            """)
        });

        var service = CreateService(
            handler,
            budgetService: budgetService,
            logService: new ThrowingChatbotConversationLogService(),
            contextBuilder: new FakeContextBuilder());

        var result = await service.GenerateReplyAsync("Where are updates?");

        Assert.That(result.Message, Is.EqualTo("Use /news to follow updates."));
        Assert.That(budgetService.UsageRecords, Has.Count.EqualTo(1));
    }

    private static SiteChatbotService CreateService(
        HttpMessageHandler handler,
        IChatbotBudgetService? budgetService = null,
        IChatbotConversationLogService? logService = null,
        ISiteChatbotContextBuilder? contextBuilder = null,
        OpenAiChatSettings? openAiSettings = null)
    {
        return new SiteChatbotService(
            new HttpClient(handler),
            budgetService ?? new FakeChatbotBudgetService(),
            logService ?? new FakeChatbotConversationLogService(),
            contextBuilder ?? new FakeContextBuilder(),
            Options.Create(openAiSettings ?? new OpenAiChatSettings { ApiKey = "test-key" }),
            NullLogger<SiteChatbotService>.Instance);
    }

    private sealed class FakeContextBuilder : ISiteChatbotContextBuilder
    {
        public string SiteContext { get; set; } = "Releases: /releases";

        public int BuildCallCount { get; private set; }

        public Task<string> BuildAsync(CancellationToken cancellationToken = default)
        {
            BuildCallCount++;
            return Task.FromResult(SiteContext);
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
        public ChatbotBudgetSummary Summary { get; set; } = new();

        public int GetSummaryCallCount { get; private set; }

        public List<(string Model, string ErrorCode, string ErrorMessage)> DisableForQuotaRecords { get; } = [];

        public List<(string Model, string ErrorCode, string ErrorMessage)> FailureRecords { get; } = [];

        public List<(string Model, long InputTokens, long OutputTokens, long CachedInputTokens)> UsageRecords { get; } = [];

        public Task DisableForQuotaErrorAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
        {
            DisableForQuotaRecords.Add((model, errorCode, errorMessage));
            return Task.CompletedTask;
        }

        public Task RecordFailureAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
        {
            FailureRecords.Add((model, errorCode, errorMessage));
            return Task.CompletedTask;
        }

        public Task<ChatbotBudgetSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            GetSummaryCallCount++;
            return Task.FromResult(Summary);
        }

        public Task<ChatbotBudgetSummary> RecordUsageAsync(string model, long inputTokens, long outputTokens, long cachedInputTokens, CancellationToken cancellationToken = default)
        {
            UsageRecords.Add((model, inputTokens, outputTokens, cachedInputTokens));
            return Task.FromResult(new ChatbotBudgetSummary());
        }

        public Task ResetAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ChatbotBudgetSummary> SetManualDisabledAsync(bool isDisabled, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatbotBudgetSummary());
    }

    private sealed class FakeChatbotConversationLogService : IChatbotConversationLogService
    {
        public List<(string UserMessage, string AssistantReply, string Outcome, ChatbotPageContext? CurrentPage)> Records { get; } = [];

        public Task RecordAsync(string userMessage, string assistantReply, string outcome, ChatbotPageContext? currentPage = null, CancellationToken cancellationToken = default)
        {
            Records.Add((userMessage, assistantReply, outcome, currentPage));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ChatbotConversationLogEntry>> GetRecentAsync(int count = 25, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ChatbotConversationLogEntry>>([]);

        public Task<IReadOnlyList<ChatbotConversationLogEntry>> GetExportAsync(int count = 5000, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ChatbotConversationLogEntry>>([]);
    }

    private sealed class ThrowingChatbotConversationLogService : IChatbotConversationLogService
    {
        public Task RecordAsync(string userMessage, string assistantReply, string outcome, ChatbotPageContext? currentPage = null, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Logging unavailable.");

        public Task<IReadOnlyList<ChatbotConversationLogEntry>> GetRecentAsync(int count = 25, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ChatbotConversationLogEntry>>([]);

        public Task<IReadOnlyList<ChatbotConversationLogEntry>> GetExportAsync(int count = 5000, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ChatbotConversationLogEntry>>([]);
    }
}
