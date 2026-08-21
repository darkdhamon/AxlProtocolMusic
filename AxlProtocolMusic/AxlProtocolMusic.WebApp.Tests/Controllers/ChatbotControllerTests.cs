using AxlProtocolMusic.WebApp.Controllers;
using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Services;
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
    public async Task PostMessage_WhenMessageIsTooLong_ReturnsBadRequestAndDoesNotCallService()
    {
        var chatbotService = new FakeSiteChatbotService();
        var controller = CreateController(chatbotService);

        var result = await controller.PostMessage(
            new ChatbotMessageRequest
            {
                Message = new string('x', 1001),
                History = []
            },
            CancellationToken.None);

        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(badRequestResult.Value?.ToString(), Does.Contain("Message too long"));
        Assert.That(chatbotService.CallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PostMessage_WhenHistoryIsTooLong_ReturnsBadRequestAndDoesNotCallService()
    {
        var chatbotService = new FakeSiteChatbotService();
        var controller = CreateController(chatbotService);
        var longHistory = new List<ChatbotConversationMessage>();

        for (var i = 0; i < 41; i++)
        {
            longHistory.Add(new ChatbotConversationMessage { Role = "user", Content = $"Message {i}" });
        }

        var result = await controller.PostMessage(
            new ChatbotMessageRequest
            {
                Message = "What changed?",
                History = longHistory
            },
            CancellationToken.None);

        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(badRequestResult.Value?.ToString(), Does.Contain("History too long"));
        Assert.That(chatbotService.CallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PostMessage_WhenHistoryEntryIsTooLong_ReturnsBadRequestAndDoesNotCallService()
    {
        var chatbotService = new FakeSiteChatbotService();
        var controller = CreateController(chatbotService);

        var result = await controller.PostMessage(
            new ChatbotMessageRequest
            {
                Message = "What changed?",
                History =
                [
                    new ChatbotConversationMessage { Role = "user", Content = new string('x', 801) }
                ]
            },
            CancellationToken.None);

        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.Value?.ToString(), Does.Contain("History entry invalid or too long"));
        Assert.That(chatbotService.CallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PostMessage_WhenHistoryContainsNullEntry_ReturnsBadRequestAndDoesNotCallService()
    {
        var chatbotService = new FakeSiteChatbotService();
        var controller = CreateController(chatbotService);

        var result = await controller.PostMessage(
            new ChatbotMessageRequest
            {
                Message = "Hello",
                History = [null!]
            },
            CancellationToken.None);

        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.Value?.ToString(), Does.Contain("History entry invalid or too long"));
        Assert.That(chatbotService.CallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PostMessage_WhenHistoryCollectionIsNull_ReturnsBadRequestAndDoesNotCallService()
    {
        var chatbotService = new FakeSiteChatbotService();
        var controller = CreateController(chatbotService);

        var result = await controller.PostMessage(
            new ChatbotMessageRequest { Message = "Hello", History = null! },
            CancellationToken.None);

        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult?.Value?.ToString(), Does.Contain("History is required"));
        Assert.That(chatbotService.CallCount, Is.Zero);
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

    [Test]
    public async Task AcquirePermit_WhenDeviceIdTrackingIsDefaultEnabled_RequiresCookieRoundTripBeforePermit()
    {
        var chatbotService = new FakeSiteChatbotService();
        var limiter = new FakeChatbotRequestRateLimiter();
        var controller = CreateController(chatbotService, limiter);
        controller.Request.Headers.Cookie = string.Empty;
        controller.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        var result = await controller.AcquirePermit(CancellationToken.None);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(StatusCodes.Status428PreconditionRequired));
        Assert.That(limiter.IssuedDeviceIds, Is.Empty);
        Assert.That(controller.Response.Headers.SetCookie.ToString(), Does.Contain("axl_visitor_id="));
    }

    [Test]
    public async Task AcquirePermit_WhenSameOriginHeaderIsMissing_ReturnsBadRequestWithoutSpendingPermit()
    {
        var chatbotService = new FakeSiteChatbotService();
        var limiter = new FakeChatbotRequestRateLimiter();
        var controller = CreateController(chatbotService, limiter);

        Assert.That(await controller.AcquirePermit(CancellationToken.None), Is.TypeOf<BadRequestObjectResult>());
        Assert.That(limiter.IssuedDeviceIds, Is.Empty);
    }

    [Test]
    public async Task AcquirePermit_WhenDeviceIdTrackingIsDisabled_ReturnsForbidden()
    {
        var limiter = new FakeChatbotRequestRateLimiter();
        var controller = CreateController(new FakeSiteChatbotService(), limiter);
        controller.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        controller.Request.Headers.Cookie = "axl_site_metrics=disabled";

        var result = await controller.AcquirePermit(CancellationToken.None);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        Assert.That(limiter.IssuedDeviceIds, Is.Empty);
    }

    [Test]
    public async Task AcquirePermit_WhenDeviceIdCookieExists_PartitionsByThatDeviceId()
    {
        var limiter = new FakeChatbotRequestRateLimiter();
        var controller = CreateController(new FakeSiteChatbotService(), limiter);
        controller.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        controller.Request.Headers.Cookie = "axl_visitor_id=device-123";

        var result = await controller.AcquirePermit(CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        Assert.That(limiter.IssuedDeviceIds, Is.EqualTo(new[] { "device-123" }));
        Assert.That(controller.Response.Headers.SetCookie.ToString(), Is.Empty);
    }

    private static ChatbotController CreateController(
        FakeSiteChatbotService chatbotService,
        IChatbotRequestRateLimiter? requestRateLimiter = null)
    {
        var controller = new ChatbotController(chatbotService, requestRateLimiter ?? new FakeChatbotRequestRateLimiter())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers.Cookie = "axl_visitor_id=test-device";
        return controller;
    }

    private sealed class FakeChatbotRequestRateLimiter : IChatbotRequestRateLimiter
    {
        public List<string> IssuedDeviceIds { get; } = [];

        public Task<string?> TryIssuePermitAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            IssuedDeviceIds.Add(deviceId);
            return Task.FromResult<string?>("permit");
        }

        public Task<bool> TryConsumePermitAsync(string permitToken, CancellationToken cancellationToken = default)
            => Task.FromResult(permitToken == "permit");
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
