using System.Security.Claims;
using AxlProtocolMusic.WebApp.Controllers;
using AxlProtocolMusic.WebApp.Models.Analytics;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Tests.Controllers;

[TestFixture]
public sealed class AnalyticsControllerTests
{
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
        controller.HttpContext.Request.Headers.Cookie = "axl_visitor_id=visitor-123";
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
        Assert.That(metric.ClientId, Is.EqualTo("visitor-123"));
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
    public async Task RecordExternalLinkClick_WhenRequestIsValid_CreatesVisitorCookieAndRecordsMetric()
    {
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(analyticsService, isHttps: true);
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
        Assert.That(metric.ClientId, Has.Length.EqualTo(32));
        Assert.That(metric.ApproximateLatitude, Is.Null);
        Assert.That(metric.ApproximateLongitude, Is.EqualTo(-97.7431));
        Assert.That(analyticsService.LastExternalClickCancellationToken, Is.EqualTo(cancellationTokenSource.Token));

        var setCookieHeader = controller.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.That(setCookieHeader, Does.Contain("axl_visitor_id="));
        Assert.That(setCookieHeader, Does.Contain("httponly"));
        Assert.That(setCookieHeader, Does.Contain("secure"));
    }

    private static AnalyticsController CreateController(FakeAnalyticsService analyticsService, bool isHttps = false)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = isHttps ? "https" : "http";

        return new AnalyticsController(analyticsService)
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
            => throw new NotSupportedException();

        public Task DeleteVisitorLocationDataAsync(string clientId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AnalyticsDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<VisitorCollectedDataViewModel> GetVisitorCollectedDataAsync(string clientId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
