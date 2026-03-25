using AxlProtocolMusic.WebApp.Controllers;
using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

    private static AdminController CreateController(FakeChatbotBudgetService chatbotBudgetService)
    {
        return new AdminController(chatbotBudgetService)
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
}
