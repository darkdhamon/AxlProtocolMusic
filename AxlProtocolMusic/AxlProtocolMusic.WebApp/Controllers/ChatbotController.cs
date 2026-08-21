using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private const string VisitorCookieName = "axl_visitor_id";
    private const string MetricsPreferenceCookieName = "axl_site_metrics";
    private const int MaxMessageLength = 1000;
    private const int MaxHistoryLength = 40;
    private const int MaxHistoryMessageLength = 800;

    private readonly ISiteChatbotService _siteChatbotService;
    private readonly IChatbotRequestRateLimiter _requestRateLimiter;
    private readonly IDeviceIdService _deviceIdService;

    public ChatbotController(
        ISiteChatbotService siteChatbotService,
        IChatbotRequestRateLimiter requestRateLimiter,
        IDeviceIdService deviceIdService)
    {
        _siteChatbotService = siteChatbotService;
        _requestRateLimiter = requestRateLimiter;
        _deviceIdService = deviceIdService;
    }

    [HttpPost("message")]
    [RequestSizeLimit(64 * 1024)]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<ChatbotMessageResponse>> PostMessage(
        [FromBody] ChatbotMessageRequest request,
        CancellationToken cancellationToken)
    {
        var (deviceId, trackingDisabled) = GetEstablishedDeviceId();
        if (deviceId is null)
        {
            return trackingDisabled
                ? StatusCode(StatusCodes.Status403Forbidden, new { error = "Device-ID tracking is required to use AI chat." })
                : StatusCode(StatusCodes.Status428PreconditionRequired, new { error = "Retry after the device-ID cookie is established." });
        }

        var permitToken = await _requestRateLimiter.TryIssuePermitAsync(deviceId, cancellationToken);
        if (permitToken is null || !await _requestRateLimiter.TryConsumePermitAsync(permitToken, cancellationToken))
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

        if (request.History is null)
        {
            return BadRequest(new { error = "History is required." });
        }

        if (request.History.Count > MaxHistoryLength)
        {
            return BadRequest(new { error = $"History too long. Maximum {MaxHistoryLength} entries." });
        }

        if (request.History.Any(item => item is null || item.Content?.Length > MaxHistoryMessageLength))
        {
            return BadRequest(new { error = $"History entry invalid or too long. Maximum {MaxHistoryMessageLength} characters." });
        }

        var response = await _siteChatbotService.GenerateReplyAsync(
            request.Message,
            request.History,
            request.CurrentPage,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("permit")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AcquirePermit(CancellationToken cancellationToken)
    {
        if (!string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal))
        {
            return BadRequest(new { error = "Same-origin request required." });
        }

        var (deviceId, trackingDisabled) = GetEstablishedDeviceId();
        if (deviceId is null)
        {
            return trackingDisabled
                ? StatusCode(StatusCodes.Status403Forbidden, new { error = "Device-ID tracking is required to use AI chat." })
                : StatusCode(StatusCodes.Status428PreconditionRequired, new { error = "Retry after the device-ID cookie is established." });
        }

        var permitToken = await _requestRateLimiter.TryIssuePermitAsync(deviceId, cancellationToken);
        if (permitToken is not null)
        {
            return Ok(new { permitToken });
        }

        Response.Headers.RetryAfter = "60";
        return StatusCode(StatusCodes.Status429TooManyRequests);
    }

    private (string? DeviceId, bool TrackingDisabled) GetEstablishedDeviceId()
    {
        if (Request.Cookies.TryGetValue(MetricsPreferenceCookieName, out var preference)
            && string.Equals(preference, "disabled", StringComparison.OrdinalIgnoreCase))
        {
            return (null, true);
        }

        if (Request.Cookies.TryGetValue(VisitorCookieName, out var existing)
            && _deviceIdService.TryResolve(existing, out var canonicalDeviceId))
        {
            return (canonicalDeviceId, false);
        }

        var canonicalVisitorId = Guid.TryParseExact(existing, "N", out _)
            ? existing
            : Guid.NewGuid().ToString("N");
        var deviceId = _deviceIdService.Protect(canonicalVisitorId);
        Response.Cookies.Append(VisitorCookieName, deviceId, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            Path = "/",
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddYears(2)
        });
        return (null, false);
    }

}
