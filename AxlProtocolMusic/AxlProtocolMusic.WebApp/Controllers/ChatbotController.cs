using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private const int MaxMessageLength = 1000;
    private const int MaxHistoryLength = 40;

    private readonly ISiteChatbotService _siteChatbotService;

    public ChatbotController(ISiteChatbotService siteChatbotService)
    {
        _siteChatbotService = siteChatbotService;
    }

    [HttpPost("message")]
    [EnableRateLimiting("chatbot-abuse")]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<ChatbotMessageResponse>> PostMessage(
        [FromBody] ChatbotMessageRequest request,
        CancellationToken cancellationToken)
    {
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

        var response = await _siteChatbotService.GenerateReplyAsync(
            request.Message,
            request.History,
            request.CurrentPage,
            cancellationToken);

        return Ok(response);
    }
}
