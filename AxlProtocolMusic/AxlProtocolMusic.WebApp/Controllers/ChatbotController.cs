using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private const int MaxMessageLength = 1000;
    private const int MaxHistoryLength = 40;
    private const int MaxHistoryMessageLength = 800;

    private readonly ISiteChatbotService _siteChatbotService;
    private readonly IChatbotRequestRateLimiter _requestRateLimiter;

    public ChatbotController(
        ISiteChatbotService siteChatbotService,
        IChatbotRequestRateLimiter requestRateLimiter)
    {
        _siteChatbotService = siteChatbotService;
        _requestRateLimiter = requestRateLimiter;
    }

    [HttpPost("message")]
    [RequestSizeLimit(64 * 1024)]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<ChatbotMessageResponse>> PostMessage(
        [FromBody] ChatbotMessageRequest request,
        CancellationToken cancellationToken)
    {
        var partitionKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        if (!_requestRateLimiter.TryAcquire(partitionKey))
        {
            Response.Headers.RetryAfter = "60";
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new { error = "Too many requests. Please wait before trying again." });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "A message is required." });
        }

        if (request.Message.Length > MaxMessageLength)
        {
            return BadRequest(new { error = $"Message too long. Maximum {MaxMessageLength} characters." });
        }

        if (request.History.Count > MaxHistoryLength)
        {
            return BadRequest(new { error = $"History too long. Maximum {MaxHistoryLength} entries." });
        }

        if (request.History.Any(item => item.Content?.Length > MaxHistoryMessageLength))
        {
            return BadRequest(new { error = $"History entry too long. Maximum {MaxHistoryMessageLength} characters." });
        }

        var response = await _siteChatbotService.GenerateReplyAsync(
            request.Message,
            request.History,
            request.CurrentPage,
            cancellationToken);

        return Ok(response);
    }
}
