using System.Security.Cryptography;
using AxlProtocolMusic.WebApp.Models.Analytics;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("analytics")]
public sealed class AnalyticsController : Controller
{
    private const string VisitorCookieName = "axl_visitor_id";
    private const string MetricsPreferenceCookieName = "axl_site_metrics";
    private const string AdminVisitorCookieName = "axl_admin_visitor";
    private const string DeviceIdProtectionPurpose = "AxlProtocolMusic.DeviceId.v1";
    private readonly IAnalyticsService _analyticsService;
    private readonly IDataProtector _deviceIdProtector;

    public AnalyticsController(IAnalyticsService analyticsService, IDataProtectionProvider dataProtectionProvider)
    {
        _analyticsService = analyticsService;
        _deviceIdProtector = dataProtectionProvider.CreateProtector(DeviceIdProtectionPurpose);
    }

    [HttpPost("page-visit")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RecordPageVisit([FromBody] PageVisitRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PagePath) || request.DurationSeconds < 0)
        {
            return BadRequest();
        }

        if (IsMetricsDisabled(HttpContext))
        {
            return Ok();
        }

        if (IsAdminTraffic(HttpContext))
        {
            return Ok();
        }

        var clientId = await GetOrCreateVisitorIdAsync(HttpContext, cancellationToken);
        if (clientId is null)
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired);
        }

        var metric = new PageVisitMetric
        {
            PagePath = request.PagePath.Trim(),
            PageTitle = request.PageTitle?.Trim() ?? string.Empty,
            DurationSeconds = request.DurationSeconds,
            VisitedAtUtc = DateTimeOffset.UtcNow,
            ClientId = clientId,
            Region = ResolveRegion(HttpContext, request.ApproximateLocation),
            ApproximateLatitude = NormalizeApproximateCoordinate(request.ApproximateLatitude, -90, 90),
            ApproximateLongitude = NormalizeApproximateCoordinate(request.ApproximateLongitude, -180, 180),
            ReferrerPath = request.ReferrerPath?.Trim() ?? string.Empty
        };

        await _analyticsService.RecordPageVisitAsync(metric, cancellationToken);
        return Ok();
    }

    [HttpPost("external-link-click")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RecordExternalLinkClick([FromBody] ExternalLinkClickRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SourcePagePath) || string.IsNullOrWhiteSpace(request.DestinationUrl))
        {
            return BadRequest();
        }

        if (IsMetricsDisabled(HttpContext))
        {
            return Ok();
        }

        if (IsAdminTraffic(HttpContext))
        {
            return Ok();
        }

        var clientId = await GetOrCreateVisitorIdAsync(HttpContext, cancellationToken);
        if (clientId is null)
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired);
        }
        var metric = new ExternalLinkClickMetric
        {
            SourcePagePath = request.SourcePagePath.Trim(),
            DestinationUrl = request.DestinationUrl.Trim(),
            LinkLabel = request.LinkLabel?.Trim() ?? string.Empty,
            ClickedAtUtc = DateTimeOffset.UtcNow,
            ClientId = clientId,
            Region = ResolveRegion(HttpContext, request.ApproximateLocation),
            ApproximateLatitude = NormalizeApproximateCoordinate(request.ApproximateLatitude, -90, 90),
            ApproximateLongitude = NormalizeApproximateCoordinate(request.ApproximateLongitude, -180, 180)
        };

        await _analyticsService.RecordExternalLinkClickAsync(metric, cancellationToken);
        return Ok();
    }

    private async Task<string?> GetOrCreateVisitorIdAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (httpContext.Request.Cookies.TryGetValue(VisitorCookieName, out var existingCookie)
            && IsValidDeviceId(existingCookie))
        {
            return existingCookie;
        }

        if (Guid.TryParseExact(existingCookie, "N", out _))
        {
            await _analyticsService.DeleteVisitorDataAsync(existingCookie, cancellationToken);
        }

        var visitorId = _deviceIdProtector.Protect(Guid.NewGuid().ToString("N"));
        httpContext.Response.Cookies.Append(
            VisitorCookieName,
            visitorId,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = httpContext.Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(2)
            });

        return null;
    }

    private bool IsValidDeviceId(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        try
        {
            return Guid.TryParseExact(_deviceIdProtector.Unprotect(deviceId), "N", out _);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static string ResolveRegion(HttpContext httpContext, string? approximateLocation)
    {
        var normalizedApproximateLocation = approximateLocation?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedApproximateLocation))
        {
            return normalizedApproximateLocation.Length > 120
                ? normalizedApproximateLocation[..120]
                : normalizedApproximateLocation;
        }

        var headers = httpContext.Request.Headers;

        var regionHeaderCandidates = new[]
        {
            "CF-IPCountry",
            "X-AppEngine-Country",
            "X-Azure-ClientRegion",
            "X-Geo-Country"
        };

        foreach (var headerName in regionHeaderCandidates)
        {
            var value = headers[headerName].ToString().Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "Unknown";
    }

    private static bool IsMetricsDisabled(HttpContext httpContext)
    {
        return httpContext.Request.Cookies.TryGetValue(MetricsPreferenceCookieName, out var value)
            && string.Equals(value, "disabled", StringComparison.OrdinalIgnoreCase);
    }

    private static double? NormalizeApproximateCoordinate(double? value, double min, double max)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
        {
            return null;
        }

        return value.Value < min || value.Value > max
            ? null
            : value.Value;
    }

    private static bool IsAdminTraffic(HttpContext httpContext)
    {
        if (httpContext.User.IsInRole("Admin"))
        {
            return true;
        }

        return httpContext.Request.Cookies.TryGetValue(AdminVisitorCookieName, out var value)
            && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
