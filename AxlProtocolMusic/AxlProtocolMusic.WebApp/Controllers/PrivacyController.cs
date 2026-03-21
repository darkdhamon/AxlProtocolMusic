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

    public PrivacyController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpPost("essential-metrics")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SetEssentialMetricsPreference([FromBody] EssentialMetricsPreferenceRequest request, CancellationToken cancellationToken)
    {
        if (request.AllowEssentialSiteMetrics)
        {
            Response.Cookies.Delete(MetricsPreferenceCookieName);
            return Ok();
        }

        if (Request.Cookies.TryGetValue(VisitorCookieName, out var visitorId)
            && !string.IsNullOrWhiteSpace(visitorId))
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
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });

        Response.Cookies.Delete(VisitorCookieName);
        return Ok();
    }

    [HttpPost("delete-my-data")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMyData(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(VisitorCookieName, out var visitorId)
            && !string.IsNullOrWhiteSpace(visitorId))
        {
            await _analyticsService.DeleteVisitorDataAsync(visitorId, cancellationToken);
        }

        return Redirect("/privacy/collected-data?deleted=true");
    }

    public sealed class EssentialMetricsPreferenceRequest
    {
        public bool AllowEssentialSiteMetrics { get; set; }
    }
}
