using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("privacy")]
public sealed class PrivacyController : Controller
{
    private const string VisitorCookieName = "axl_visitor_id";
    private const string MetricsPreferenceCookieName = "axl_site_metrics";
    private readonly IAnalyticsService _analyticsService;
    private readonly IDeviceIdService _deviceIdService;

    public PrivacyController(IAnalyticsService analyticsService, IDeviceIdService deviceIdService)
    {
        _analyticsService = analyticsService;
        _deviceIdService = deviceIdService;
    }

    [HttpPost("essential-metrics")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetEssentialMetricsPreference([FromBody] EssentialMetricsPreferenceRequest request, CancellationToken cancellationToken)
    {
        if (request.AllowEssentialSiteMetrics)
        {
            Response.Cookies.Delete(MetricsPreferenceCookieName, new CookieOptions { Path = "/" });
            return Ok();
        }

        if (TryResolveVisitorId(out var visitorId))
        {
            await _analyticsService.DeleteVisitorDataAsync(visitorId, cancellationToken);
        }

        Response.Cookies.Append(
            MetricsPreferenceCookieName,
            "disabled",
            new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(2)
            });

        Response.Cookies.Delete(VisitorCookieName, new CookieOptions { Path = "/" });
        return Ok();
    }

    [HttpPost("delete-my-data")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMyData(CancellationToken cancellationToken)
    {
        if (TryResolveVisitorId(out var visitorId))
        {
            await _analyticsService.DeleteVisitorDataAsync(visitorId, cancellationToken);
        }

        return Redirect("/privacy/collected-data?deleted=true");
    }

    private bool TryResolveVisitorId(out string visitorId)
    {
        Request.Cookies.TryGetValue(VisitorCookieName, out var cookieValue);
        if (_deviceIdService.TryResolve(cookieValue, out visitorId))
        {
            return true;
        }

        visitorId = Guid.TryParseExact(cookieValue, "N", out _) ? cookieValue : string.Empty;
        return visitorId.Length > 0;
    }

    public sealed class EssentialMetricsPreferenceRequest
    {
        public bool AllowEssentialSiteMetrics { get; set; }
    }
}
