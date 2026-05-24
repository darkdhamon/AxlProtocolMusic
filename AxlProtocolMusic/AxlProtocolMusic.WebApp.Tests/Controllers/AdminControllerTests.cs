using AxlProtocolMusic.WebApp.Controllers;
using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AxlProtocolMusic.WebApp.Tests.Controllers;

[TestFixture]
public sealed class AdminControllerTests
{
    [Test]
    public async Task ResetChatbotBudget_CallsResetAndRedirects()
    {
        var chatbotBudgetService = new FakeChatbotBudgetService();
        var controller = CreateController(chatbotBudgetService);
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await controller.ResetChatbotBudget(cancellationTokenSource.Token);

        Assert.That(chatbotBudgetService.ResetCallCount, Is.EqualTo(1));
        Assert.That(chatbotBudgetService.LastResetCancellationToken, Is.EqualTo(cancellationTokenSource.Token));

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult!.Url, Is.EqualTo("/admin?chatbotReset=true"));
    }

    [Test]
    public async Task DisableChatbot_SetsManualDisableTrueAndRedirects()
    {
        var chatbotBudgetService = new FakeChatbotBudgetService();
        var controller = CreateController(chatbotBudgetService);
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await controller.DisableChatbot(cancellationTokenSource.Token);

        Assert.That(chatbotBudgetService.LastManualDisabledValue, Is.True);
        Assert.That(chatbotBudgetService.LastManualDisabledCancellationToken, Is.EqualTo(cancellationTokenSource.Token));

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult!.Url, Is.EqualTo("/admin?chatbotOverrideChanged=true"));
    }

    [Test]
    public async Task EnableChatbot_SetsManualDisableFalseAndRedirects()
    {
        var chatbotBudgetService = new FakeChatbotBudgetService();
        var controller = CreateController(chatbotBudgetService);
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await controller.EnableChatbot(cancellationTokenSource.Token);

        Assert.That(chatbotBudgetService.LastManualDisabledValue, Is.False);
        Assert.That(chatbotBudgetService.LastManualDisabledCancellationToken, Is.EqualTo(cancellationTokenSource.Token));

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult!.Url, Is.EqualTo("/admin?chatbotOverrideChanged=true"));
    }

    [Test]
    public async Task DownloadChatbotMessagesCsv_ReturnsEscapedCsvFile()
    {
        var chatbotBudgetService = new FakeChatbotBudgetService();
        var chatbotConversationLogService = new FakeChatbotConversationLogService
        {
            Entries =
            [
                new ChatbotConversationLogEntry
                {
                    Id = "log-1",
                    CreatedAtUtc = new DateTimeOffset(2026, 4, 2, 18, 30, 0, TimeSpan.Zero),
                    Outcome = "completed",
                    PagePath = "/releases/signals",
                    PageTitle = "Signals",
                    UserMessage = "Hello, \"admin\"",
                    AssistantReply = "Line one\r\nLine two"
                }
            ]
        };
        var controller = CreateController(chatbotBudgetService, chatbotConversationLogService);
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await controller.DownloadChatbotMessagesCsv(cancellationTokenSource.Token);

        Assert.That(chatbotConversationLogService.LastExportCancellationToken, Is.EqualTo(cancellationTokenSource.Token));

        var fileResult = result as FileContentResult;
        Assert.That(fileResult, Is.Not.Null);
        Assert.That(fileResult!.ContentType, Is.EqualTo("text/csv; charset=utf-8"));
        Assert.That(fileResult.FileDownloadName, Does.StartWith("chatbot-messages-"));
        Assert.That(fileResult.FileDownloadName, Does.EndWith(".csv"));

        var csv = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.That(csv, Does.Contain("CreatedAtUtc,Outcome,PagePath,PageTitle,UserMessage,AssistantReply"));
        Assert.That(csv, Does.Contain("\"Hello, \"\"admin\"\"\""));
        Assert.That(csv, Does.Contain("\"Line one\r\nLine two\""));
    }

    private static AdminController CreateController(
        FakeChatbotBudgetService chatbotBudgetService,
        FakeChatbotConversationLogService? chatbotConversationLogService = null)
    {
        return new AdminController(
            chatbotBudgetService,
            chatbotConversationLogService ?? new FakeChatbotConversationLogService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private sealed class FakeChatbotBudgetService : IChatbotBudgetService
    {
        public int ResetCallCount { get; private set; }

        public CancellationToken LastResetCancellationToken { get; private set; }

        public bool? LastManualDisabledValue { get; private set; }

        public CancellationToken LastManualDisabledCancellationToken { get; private set; }

        public Task<ChatbotBudgetSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ChatbotBudgetSummary> RecordUsageAsync(string model, long inputTokens, long outputTokens, long cachedInputTokens, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DisableForQuotaErrorAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task RecordFailureAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ResetCallCount++;
            LastResetCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<ChatbotBudgetSummary> SetManualDisabledAsync(bool isDisabled, CancellationToken cancellationToken = default)
        {
            LastManualDisabledValue = isDisabled;
            LastManualDisabledCancellationToken = cancellationToken;
            return Task.FromResult(new ChatbotBudgetSummary
            {
                IsManuallyDisabled = isDisabled
            });
        }
    }

    private sealed class FakeChatbotConversationLogService : IChatbotConversationLogService
    {
        public IReadOnlyList<ChatbotConversationLogEntry> Entries { get; set; } = [];

        public CancellationToken LastExportCancellationToken { get; private set; }

        public Task RecordAsync(string userMessage, string assistantReply, string outcome, ChatbotPageContext? currentPage = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ChatbotConversationLogEntry>> GetRecentAsync(int count = 25, CancellationToken cancellationToken = default)
            => Task.FromResult(Entries);

        public Task<IReadOnlyList<ChatbotConversationLogEntry>> GetExportAsync(int count = 5000, CancellationToken cancellationToken = default)
        {
            LastExportCancellationToken = cancellationToken;
            return Task.FromResult(Entries);
        }
    }
}
