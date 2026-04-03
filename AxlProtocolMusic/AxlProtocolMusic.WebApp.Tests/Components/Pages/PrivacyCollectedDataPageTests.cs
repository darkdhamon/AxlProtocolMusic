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

    [Test]
    public void PrivacyCollectedData_WhenHttpContextIsMissing_RendersDefaultEmptyState()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService());
        context.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());

        var cut = context.Render<PrivacyCollectedData>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Visitor Identifier:</strong> Not set"));
            Assert.That(cut.Markup, Does.Contain("No Visitor Data Yet"));
            Assert.That(cut.Markup, Does.Contain("Page Visits:</strong> 0"));
            Assert.That(cut.Markup, Does.Contain("External Link Clicks:</strong> 0"));
        });
    }

    [Test]
    public void PrivacyCollectedData_WhenVisitorHasIdentifierButNoStoredDetails_ShowsNoDataMessagesAndDeleteModalCanClose()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService
        {
            VisitorData = new VisitorCollectedDataViewModel
            {
                VisitorId = "visitor-456"
            }
        });
        context.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "axl_visitor_id=visitor-456";
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var cut = context.Render<PrivacyCollectedData>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Delete My Data"));
            Assert.That(cut.Markup, Does.Contain("No mappable coarse location points are stored for this browser yet."));
            Assert.That(cut.Markup, Does.Contain("No page visit records are currently stored for this browser."));
            Assert.That(cut.Markup, Does.Contain("No external link click records are currently stored for this browser."));
        });

        cut.Find("button.btn.btn-outline-danger").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Delete Collected Data"));
            Assert.That(cut.Markup, Does.Contain("Yes, Delete My Data"));
        });

        cut.Find("button.btn.btn-secondary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Not.Contain("Yes, Delete My Data"));
            Assert.That(cut.Markup, Does.Contain("Delete My Data"));
        });
    }

    [Test]
    public void PrivacyCollectedData_WhenManyRegionsAndLinksExist_RendersNormalizedLabelsAndDestinationFallbacks()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService
        {
            VisitorData = new VisitorCollectedDataViewModel
            {
                VisitorId = "visitor-789",
                PageVisits =
                [
                    CreateVisit("/al", "Alabama Story", 42, "Township of Birmingham, Alabama, United States"),
                    CreateVisit("/ak", "Alaska Story", 125, "Anchorage, Alaska, United States"),
                    CreateVisit("/az", "Arizona Story", 3665, "Phoenix, Arizona, United States"),
                    CreateVisit("/ar", "Arkansas Story", 180, "Little Rock, Arkansas, United States"),
                    CreateVisit("/ca", "California Story", 75, "Los Angeles Township, California, United Kingdom of Great Britain and Northern Ireland (the)"),
                    CreateVisit("/co", "Colorado Story", 61, "Denver, Colorado, United States"),
                    CreateVisit("/ct", "Connecticut Story", 88, "Hartford, Connecticut, United States"),
                    CreateVisit("/de", "Delaware Story", 95, "Dover, Delaware, United States"),
                    CreateVisit("/fl", "Florida Story", 120, "Miami, Florida, United States"),
                    CreateVisit("/ga", "Georgia Story", 240, "Atlanta, Georgia, United States")
                ],
                ExternalLinkClicks =
                [
                    CreateClick("/hi", "Hawaii Link", "https://www.bandcamp.com/album/waves", "Honolulu, Hawaii, United States"),
                    CreateClick("/id", string.Empty, "https://example.com/", "Boise, Idaho, United States"),
                    CreateClick("/il", string.Empty, "https://open.spotify.com/track/123", "Chicago, Illinois, United States"),
                    CreateClick("/in", string.Empty, "not-a-valid-url", "Indianapolis, Indiana, United States"),
                    CreateClick("/ia", string.Empty, string.Empty, "Des Moines, Iowa, United States"),
                    CreateClick("/ks", string.Empty, "https://music.apple.com/us/album/test", "Wichita, Kansas, United States"),
                    CreateClick("/ky", "Store", "https://store.example.com/", "Louisville, Kentucky, United States"),
                    CreateClick("/la", string.Empty, "https://www.youtube.com/watch?v=123", "New Orleans, Louisiana, United States"),
                    CreateClick("/me", string.Empty, "https://soundcloud.com/test-track", "Portland, Maine, United States"),
                    CreateClick("/md", string.Empty, "https://tidal.com/browse/track/1", "Baltimore, Maryland, United States")
                ]
            }
        });
        context.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "axl_visitor_id=visitor-789";
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var cut = context.Render<PrivacyCollectedData>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Region: Birmingham, AL, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Anchorage, AK, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Phoenix, AZ, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Little Rock, AR, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Los Angeles, CA, UK"));
            Assert.That(cut.Markup, Does.Contain("Region: Denver, CO, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Hartford, CT, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Dover, DE, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Miami, FL, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Atlanta, GA, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Honolulu, HI, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Boise, ID, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Chicago, IL, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Indianapolis, IN, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Des Moines, IA, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Wichita, KS, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Louisville, KY, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: New Orleans, LA, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Portland, ME, USA"));
            Assert.That(cut.Markup, Does.Contain("Region: Baltimore, MD, USA"));
            Assert.That(cut.Markup, Does.Contain("42 sec"));
            Assert.That(cut.Markup, Does.Contain("1h 1m"));
            Assert.That(cut.Markup, Does.Contain(">example.com<"));
            Assert.That(cut.Markup, Does.Contain(">open.spotify.com/track/123<"));
            Assert.That(cut.Markup, Does.Contain(">music.apple.com/us/album/test<"));
            Assert.That(cut.Markup, Does.Contain(">Open link<"));
            Assert.That(cut.Markup, Does.Contain(">not-a-valid-url<"));
            Assert.That(cut.Markup, Does.Contain("Hawaii Link"));
            Assert.That(cut.Markup, Does.Contain("Store"));
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

    private static VisitorPageVisitViewModel CreateVisit(string path, string title, double durationSeconds, string region)
    {
        return new VisitorPageVisitViewModel
        {
            PagePath = path,
            PageTitle = title,
            DurationSeconds = durationSeconds,
            VisitedAtUtc = new DateTimeOffset(2026, 3, 20, 16, 0, 0, TimeSpan.Zero),
            Region = region,
            ApproximateLatitude = 30.2672,
            ApproximateLongitude = -97.7431,
            ReferrerPath = "/home"
        };
    }

    private static VisitorExternalLinkClickViewModel CreateClick(string sourcePagePath, string label, string destinationUrl, string region)
    {
        return new VisitorExternalLinkClickViewModel
        {
            SourcePagePath = sourcePagePath,
            DestinationUrl = destinationUrl,
            LinkLabel = label,
            ClickedAtUtc = new DateTimeOffset(2026, 3, 20, 17, 0, 0, TimeSpan.Zero),
            Region = region,
            ApproximateLatitude = 30.2672,
            ApproximateLongitude = -97.7431
        };
    }
}
