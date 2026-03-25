using AxlProtocolMusic.WebApp.Controllers;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Tests.Controllers;

[TestFixture]
public sealed class PrivacyControllerTests
{
    [Test]
    public async Task SetEssentialMetricsPreference_WhenAllowed_ReturnsOkAndDeletesPreferenceCookie()
    {
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(analyticsService);

        var result = await controller.SetEssentialMetricsPreference(
            new PrivacyController.EssentialMetricsPreferenceRequest
            {
                AllowEssentialSiteMetrics = true
            },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(analyticsService.DeletedVisitorIds, Is.Empty);
        Assert.That(
            controller.HttpContext.Response.Headers.SetCookie.ToString(),
            Does.Contain("axl_site_metrics=;"));
    }

    [Test]
    public async Task SetEssentialMetricsPreference_WhenDisabled_DeletesVisitorDataAndSetsOptOutCookie()
    {
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(analyticsService, isHttps: true);
        controller.HttpContext.Request.Headers.Cookie = "axl_visitor_id=visitor-123";

        var result = await controller.SetEssentialMetricsPreference(
            new PrivacyController.EssentialMetricsPreferenceRequest
            {
                AllowEssentialSiteMetrics = false
            },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(analyticsService.DeletedVisitorIds, Is.EqualTo(new[] { "visitor-123" }));

        var setCookieHeader = controller.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.That(setCookieHeader, Does.Contain("axl_site_metrics=disabled"));
        Assert.That(setCookieHeader, Does.Contain("secure"));
        Assert.That(setCookieHeader, Does.Contain("axl_visitor_id=;"));
    }

    [Test]
    public async Task DeleteMyData_WhenVisitorCookieExists_DeletesVisitorDataAndRedirects()
    {
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(analyticsService);
        controller.HttpContext.Request.Headers.Cookie = "axl_visitor_id=visitor-456";

        var result = await controller.DeleteMyData(CancellationToken.None);

        Assert.That(analyticsService.DeletedVisitorIds, Is.EqualTo(new[] { "visitor-456" }));

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult!.Url, Is.EqualTo("/privacy/collected-data?deleted=true"));
    }

    private static PrivacyController CreateController(FakeAnalyticsService analyticsService, bool isHttps = false)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = isHttps ? "https" : "http";

        return new PrivacyController(analyticsService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private sealed class FakeAnalyticsService : IAnalyticsService
    {
        public List<string> DeletedVisitorIds { get; } = [];

        public Task DeleteVisitorDataAsync(string clientId, CancellationToken cancellationToken = default)
        {
            DeletedVisitorIds.Add(clientId);
            return Task.CompletedTask;
        }

        public Task DeleteVisitorLocationDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<AnalyticsDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<VisitorCollectedDataViewModel> GetVisitorCollectedDataAsync(string clientId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task RecordExternalLinkClickAsync(AxlProtocolMusic.WebApp.Models.Analytics.ExternalLinkClickMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordPageVisitAsync(AxlProtocolMusic.WebApp.Models.Analytics.PageVisitMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
