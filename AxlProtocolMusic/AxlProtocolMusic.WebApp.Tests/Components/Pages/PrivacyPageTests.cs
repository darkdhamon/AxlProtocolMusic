using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Analytics;
using AxlProtocolMusic.WebApp.Models.Privacy;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class PrivacyPageTests
{
    [Test]
    public void Privacy_RendersCurrentPreferencesAndVisitorCount()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService
        {
            Summary = new AnalyticsDashboardSummary { UniqueVisitors = 1250 }
        });
        context.Services.AddSingleton<IPrivacyPreferencesService>(new FakePrivacyPreferencesService
        {
            Preferences = new PrivacyPreferences
            {
                AllowEssentialSiteMetrics = true,
                ShareApproximateLocation = true,
                AllowEnhancedEngagementMetrics = false,
                AllowPersonalizationMetrics = true
            }
        });
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });

        var cut = context.Render<Privacy>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Your Data And This Site"));
            Assert.That(cut.Markup, Does.Contain("Turning this off will stop future essential analytics for this browser"));
            Assert.That(cut.Markup, Does.Contain("See Collected Data"));
        });

        var toggles = cut.FindAll("input[type='checkbox']");
        Assert.That(toggles, Has.Count.EqualTo(4));
        Assert.That(toggles[0].HasAttribute("checked"), Is.True);
        Assert.That(toggles[0].HasAttribute("disabled"), Is.False);
        Assert.That(toggles[1].HasAttribute("checked"), Is.True);
        Assert.That(toggles[2].HasAttribute("checked"), Is.False);
        Assert.That(toggles[3].HasAttribute("checked"), Is.True);
    }

    [Test]
    public void Privacy_WhenVisitorThresholdIsLow_DisablesEssentialMetricsToggle()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService
        {
            Summary = new AnalyticsDashboardSummary { UniqueVisitors = 42 }
        });
        context.Services.AddSingleton<IPrivacyPreferencesService>(new FakePrivacyPreferencesService());
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });

        var cut = context.Render<Privacy>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Current unique visitors: 42."));
        });

        var essentialToggle = cut.FindAll("input[type='checkbox']").First();
        Assert.That(essentialToggle.HasAttribute("disabled"), Is.True);
    }

    private sealed class FakeAnalyticsService : IAnalyticsService
    {
        public AnalyticsDashboardSummary Summary { get; set; } = new();

        public Task RecordPageVisitAsync(PageVisitMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordExternalLinkClickAsync(ExternalLinkClickMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteVisitorDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteVisitorLocationDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<AnalyticsDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);

        public Task<VisitorCollectedDataViewModel> GetVisitorCollectedDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(new VisitorCollectedDataViewModel());
    }

    private sealed class FakePrivacyPreferencesService : IPrivacyPreferencesService
    {
        public PrivacyPreferences Preferences { get; set; } = new();

        public Task<PrivacyPreferences> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Preferences);

        public Task<PrivacyPreferenceSaveResult> SaveAsync(PrivacyPreferences preferences, CancellationToken cancellationToken = default)
            => Task.FromResult(new PrivacyPreferenceSaveResult { Preferences = preferences });

        public Task<PrivacyPreferenceSaveResult> SyncApproximateLocationPreferenceAsync(PrivacyPreferences preferences, CancellationToken cancellationToken = default)
            => Task.FromResult(new PrivacyPreferenceSaveResult { Preferences = preferences });
    }
}
