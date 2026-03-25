using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Analytics;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class PrivacyCollectedDataPageTests
{
    [Test]
    public void PrivacyCollectedData_WhenVisitorCookieIsMissing_RendersEmptyState()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService());
        context.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });

        var cut = context.Render<PrivacyCollectedData>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Collected Data For This Browser"));
            Assert.That(cut.Markup, Does.Contain("Visitor Identifier:</strong> Not set"));
            Assert.That(cut.Markup, Does.Contain("No Visitor Data Yet"));
        });
    }

    [Test]
    public void PrivacyCollectedData_WhenVisitorDataExists_RendersGroupedDetailsAndNotices()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService
        {
            VisitorData = new VisitorCollectedDataViewModel
            {
                VisitorId = "visitor-123",
                PageVisits =
                [
                    new VisitorPageVisitViewModel
                    {
                        PagePath = "/releases/signals",
                        PageTitle = "Signals",
                        DurationSeconds = 125,
                        VisitedAtUtc = new DateTimeOffset(2026, 3, 20, 16, 0, 0, TimeSpan.Zero),
                        Region = "Township of Austin, Texas, United States",
                        ApproximateLatitude = 30.2672,
                        ApproximateLongitude = -97.7431,
                        ReferrerPath = "/home"
                    }
                ],
                ExternalLinkClicks =
                [
                    new VisitorExternalLinkClickViewModel
                    {
                        SourcePagePath = "/releases/signals",
                        DestinationUrl = "https://open.spotify.com/track/123",
                        LinkLabel = string.Empty,
                        ClickedAtUtc = new DateTimeOffset(2026, 3, 20, 17, 0, 0, TimeSpan.Zero),
                        Region = "Austin, Texas, United States of America (the)",
                        ApproximateLatitude = 30.2672,
                        ApproximateLongitude = -97.7431
                    }
                ]
            }
        });
        context.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "axl_visitor_id=visitor-123; axl_site_metrics=disabled; axl_admin_visitor=true";
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var cut = context.Render<PrivacyCollectedData>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Visitor Identifier:</strong> visitor-123"));
            Assert.That(cut.Markup, Does.Contain("Delete My Data"));
            Assert.That(cut.Markup, Does.Contain("Essential Metrics Disabled"));
            Assert.That(cut.Markup, Does.Contain("Admin Browser Exclusion Active"));
            Assert.That(cut.Markup, Does.Contain("Signals"));
            Assert.That(cut.Markup, Does.Contain("Region: Austin, TX, USA"));
            Assert.That(cut.Markup, Does.Contain("open.spotify.com/track/123"));
            Assert.That(cut.Markup, Does.Contain("View individual visits"));
            Assert.That(cut.Markup, Does.Contain("View individual clicks"));
        });
    }

    private sealed class FakeAnalyticsService : IAnalyticsService
    {
        public VisitorCollectedDataViewModel VisitorData { get; set; } = new();

        public Task RecordPageVisitAsync(PageVisitMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordExternalLinkClickAsync(ExternalLinkClickMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteVisitorDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteVisitorLocationDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<AnalyticsDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AnalyticsDashboardSummary());

        public Task<VisitorCollectedDataViewModel> GetVisitorCollectedDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(VisitorData);
    }
}
