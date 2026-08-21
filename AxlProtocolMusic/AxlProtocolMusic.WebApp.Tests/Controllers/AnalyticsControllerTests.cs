using System.Security.Claims;
using AxlProtocolMusic.WebApp.Controllers;
using AxlProtocolMusic.WebApp.Models.Analytics;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Tests.Controllers;

[TestFixture]
public sealed class AnalyticsControllerTests
{
    private static readonly IDataProtectionProvider DataProtectionProvider = new EphemeralDataProtectionProvider();
    [Test]
    public async Task RecordPageVisit_WhenRequestIsInvalid_ReturnsBadRequest()
    {
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(analyticsService);

        var result = await controller.RecordPageVisit(
            new PageVisitRequest
            {
                PagePath = " ",
                DurationSeconds = -1
            },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestResult>());
        Assert.That(analyticsService.RecordedPageVisits, Is.Empty);
    }

    [Test]
    public async Task RecordPageVisit_WhenMetricsAreDisabled_ReturnsOkWithoutRecording()
    {
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(analyticsService);
        controller.HttpContext.Request.Headers.Cookie = "axl_site_metrics=disabled";

        var result = await controller.RecordPageVisit(
            new PageVisitRequest
            {
                PagePath = "/news",
                DurationSeconds = 12
            },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(analyticsService.RecordedPageVisits, Is.Empty);
    }

    [Test]
    public async Task RecordPageVisit_WhenRequestIsValid_RecordsNormalizedMetric()
    {
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(analyticsService, isHttps: true);
        var protectedDeviceId = DataProtectionProvider
            .CreateProtector("AxlProtocolMusic.DeviceId.v1")
            .Protect("0123456789abcdef0123456789abcdef");
        controller.HttpContext.Request.Headers.Cookie = $"axl_visitor_id={protectedDeviceId}";
        controller.HttpContext.Request.Headers["CF-IPCountry"] = "US";
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await controller.RecordPageVisit(
            new PageVisitRequest
            {
                PagePath = " /news ",
                PageTitle = " Latest News ",
                DurationSeconds = 15.5,
                ReferrerPath = " /home ",
                ApproximateLatitude = 40.7128,
                ApproximateLongitude = 999
            },
            cancellationTokenSource.Token);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(analyticsService.RecordedPageVisits, Has.Count.EqualTo(1));

        var metric = analyticsService.RecordedPageVisits.Single();
        Assert.That(metric.PagePath, Is.EqualTo("/news"));
        Assert.That(metric.PageTitle, Is.EqualTo("Latest News"));
        Assert.That(metric.DurationSeconds, Is.EqualTo(15.5));
        Assert.That(metric.ClientId, Is.EqualTo(protectedDeviceId));
        Assert.That(metric.Region, Is.EqualTo("US"));
        Assert.That(metric.ApproximateLatitude, Is.EqualTo(40.7128));
        Assert.That(metric.ApproximateLongitude, Is.Null);
        Assert.That(metric.ReferrerPath, Is.EqualTo("/home"));
        Assert.That(analyticsService.LastPageVisitCancellationToken, Is.EqualTo(cancellationTokenSource.Token));
    }

    [Test]
    public async Task RecordExternalLinkClick_WhenAdminCookieIsPresent_ReturnsOkWithoutRecording()
    {
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(analyticsService);
        controller.HttpContext.Request.Headers.Cookie = "axl_admin_visitor=true";

        var result = await controller.RecordExternalLinkClick(
            new ExternalLinkClickRequest
            {
                SourcePagePath = "/releases/signals",
                DestinationUrl = "https://example.com"
            },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(analyticsService.RecordedExternalClicks, Is.Empty);
    }

    [Test]
    public async Task RecordExternalLinkClick_WhenRequestIsValid_RecordsMetricForEstablishedVisitor()
    {
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(analyticsService, isHttps: true);
        var protectedDeviceId = DataProtectionProvider
            .CreateProtector("AxlProtocolMusic.DeviceId.v1")
            .Protect("0123456789abcdef0123456789abcdef");
        controller.Request.Headers.Cookie = $"axl_visitor_id={protectedDeviceId}";
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await controller.RecordExternalLinkClick(
            new ExternalLinkClickRequest
            {
                SourcePagePath = " /releases/signals ",
                DestinationUrl = " https://bandcamp.example/signals ",
                LinkLabel = " Bandcamp ",
                ApproximateLocation = new string('A', 130),
                ApproximateLatitude = double.NaN,
                ApproximateLongitude = -97.7431
            },
            cancellationTokenSource.Token);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(analyticsService.RecordedExternalClicks, Has.Count.EqualTo(1));

        var metric = analyticsService.RecordedExternalClicks.Single();
        Assert.That(metric.SourcePagePath, Is.EqualTo("/releases/signals"));
        Assert.That(metric.DestinationUrl, Is.EqualTo("https://bandcamp.example/signals"));
        Assert.That(metric.LinkLabel, Is.EqualTo("Bandcamp"));
        Assert.That(metric.Region.Length, Is.EqualTo(120));
        Assert.That(metric.ClientId, Is.EqualTo(protectedDeviceId));
        Assert.That(metric.ApproximateLatitude, Is.Null);
        Assert.That(metric.ApproximateLongitude, Is.EqualTo(-97.7431));
        Assert.That(analyticsService.LastExternalClickCancellationToken, Is.EqualTo(cancellationTokenSource.Token));

        var setCookieHeader = controller.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.That(setCookieHeader, Is.Empty);
    }

    [Test]
    public async Task RecordPageVisit_WhenLegacyVisitorCookieExists_RemovesLegacyDataBeforeRotation()
    {
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(analyticsService);
        const string legacyDeviceId = "0123456789abcdef0123456789abcdef";
        controller.Request.Headers.Cookie = $"axl_visitor_id={legacyDeviceId}";

        var result = await controller.RecordPageVisit(
            new PageVisitRequest { PagePath = "/privacy", DurationSeconds = 1 },
            CancellationToken.None);

        Assert.That(result, Is.TypeOf<StatusCodeResult>());
        Assert.That(((StatusCodeResult)result).StatusCode, Is.EqualTo(StatusCodes.Status428PreconditionRequired));
        Assert.That(analyticsService.DeletedVisitorIds, Is.EqualTo(new[] { legacyDeviceId }));
        Assert.That(analyticsService.RecordedPageVisits, Is.Empty);
        Assert.That(controller.Response.Headers.SetCookie.ToString(), Does.Contain("axl_visitor_id="));
        Assert.That(controller.Response.Headers.SetCookie.ToString(), Does.Contain("path=/"));
    }

    [Test]
    public async Task RecordPageVisit_WhenVisitorCookieIsMissing_RequiresCookieRoundTripWithoutRecording()
    {
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(analyticsService);

        var result = await controller.RecordPageVisit(
            new PageVisitRequest { PagePath = "/", DurationSeconds = 1 },
            CancellationToken.None);

        Assert.That(((StatusCodeResult)result).StatusCode, Is.EqualTo(StatusCodes.Status428PreconditionRequired));
        Assert.That(analyticsService.RecordedPageVisits, Is.Empty);
        Assert.That(controller.Response.Headers.SetCookie.ToString(), Does.Contain("axl_visitor_id="));
    }

    private static AnalyticsController CreateController(FakeAnalyticsService analyticsService, bool isHttps = false)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = isHttps ? "https" : "http";

        return new AnalyticsController(analyticsService, DataProtectionProvider)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private sealed class FakeAnalyticsService : IAnalyticsService
    {
        public List<PageVisitMetric> RecordedPageVisits { get; } = [];

        public List<ExternalLinkClickMetric> RecordedExternalClicks { get; } = [];
        public List<string> DeletedVisitorIds { get; } = [];

        public CancellationToken LastPageVisitCancellationToken { get; private set; }

        public CancellationToken LastExternalClickCancellationToken { get; private set; }

        public Task RecordPageVisitAsync(PageVisitMetric metric, CancellationToken cancellationToken = default)
        {
            RecordedPageVisits.Add(metric);
            LastPageVisitCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task RecordExternalLinkClickAsync(ExternalLinkClickMetric metric, CancellationToken cancellationToken = default)
        {
            RecordedExternalClicks.Add(metric);
            LastExternalClickCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task DeleteVisitorDataAsync(string clientId, CancellationToken cancellationToken = default)
        {
            DeletedVisitorIds.Add(clientId);
            return Task.CompletedTask;
        }

        public Task DeleteVisitorLocationDataAsync(string clientId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AnalyticsDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<VisitorCollectedDataViewModel> GetVisitorCollectedDataAsync(string clientId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
