using AxlProtocolMusic.WebApp.Controllers;
using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Tests.Controllers;

[TestFixture]
public sealed class ChatbotControllerTests
{
    [Test]
    public async Task PostMessage_WhenMessageIsBlank_ReturnsBadRequestAndDoesNotCallService()
    {
        var chatbotService = new FakeSiteChatbotService();
        var controller = CreateController(chatbotService);

        var result = await controller.PostMessage(
            new ChatbotMessageRequest
            {
                Message = " ",
                History =
                [
                    new ChatbotConversationMessage { Role = "user", Content = "Earlier message" }
                ]
            },
            CancellationToken.None);

        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(badRequestResult.Value?.ToString(), Does.Contain("A message is required."));
        Assert.That(chatbotService.CallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PostMessage_WhenMessageIsValid_ForwardsArgumentsAndReturnsOk()
    {
        var chatbotService = new FakeSiteChatbotService
        {
            Response = new ChatbotMessageResponse
            {
                Message = "Generated reply",
                IsEnabled = true,
                IsConfigured = true
            }
        };
        var controller = CreateController(chatbotService);
        var history = new List<ChatbotConversationMessage>
        {
            new() { Role = "assistant", Content = "Previous reply" }
        };
        var currentPage = new ChatbotPageContext
        {
            PagePath = "/news",
            PageTitle = "News",
            PageContent = "Current page summary"
        };
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await controller.PostMessage(
            new ChatbotMessageRequest
            {
                Message = "What changed?",
                History = history,
                CurrentPage = currentPage
            },
            cancellationTokenSource.Token);

        Assert.That(chatbotService.CallCount, Is.EqualTo(1));
        Assert.That(chatbotService.LastMessage, Is.EqualTo("What changed?"));
        Assert.That(chatbotService.LastHistory, Is.SameAs(history));
        Assert.That(chatbotService.LastCurrentPage, Is.SameAs(currentPage));
        Assert.That(chatbotService.LastCancellationToken, Is.EqualTo(cancellationTokenSource.Token));

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(okResult.Value, Is.SameAs(chatbotService.Response));

        Assert.That(result.Value, Is.Null);
    }

    private static ChatbotController CreateController(FakeSiteChatbotService chatbotService)
    {
        return new ChatbotController(chatbotService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private sealed class FakeSiteChatbotService : ISiteChatbotService
    {
        public int CallCount { get; private set; }

        public string? LastMessage { get; private set; }

        public IReadOnlyList<ChatbotConversationMessage>? LastHistory { get; private set; }

        public ChatbotPageContext? LastCurrentPage { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public ChatbotMessageResponse Response { get; set; } = new();

        public Task<ChatbotMessageResponse> GenerateReplyAsync(
            string message,
            IReadOnlyList<ChatbotConversationMessage>? history = null,
            ChatbotPageContext? currentPage = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastMessage = message;
            LastHistory = history;
            LastCurrentPage = currentPage;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Response);
        }
    }
}
