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

    [Test]
    public void Privacy_WhenApproximateLocationIsTurnedOff_OpensConfirmationModal()
    {
        using var context = new BunitContext();
        var analyticsService = new FakeAnalyticsService
        {
            Summary = new AnalyticsDashboardSummary { UniqueVisitors = 1250 }
        };
        var privacyService = new FakePrivacyPreferencesService
        {
            Preferences = new PrivacyPreferences
            {
                AllowEssentialSiteMetrics = true,
                ShareApproximateLocation = true
            }
        };

        context.Services.AddSingleton<IAnalyticsService>(analyticsService);
        context.Services.AddSingleton<IPrivacyPreferencesService>(privacyService);
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });

        var cut = context.Render<Privacy>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.FindAll("input[type='checkbox']"), Has.Count.EqualTo(4));
        });

        cut.FindAll("input[type='checkbox']")[1].Change(false);

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Confirm Privacy Preference Change"));
            Assert.That(cut.Markup, Does.Contain("Approximate Location Sharing"));
            Assert.That(cut.Markup, Does.Contain("remove previously stored location-related analytics"));
        });

        Assert.That(privacyService.SyncApproximateLocationCallCount, Is.EqualTo(0));
        Assert.That(analyticsService.DeletedLocationVisitorIds, Is.Empty);
    }

    [Test]
    public void Privacy_WhenApproximateLocationDisableIsConfirmed_DeletesStoredLocationDataAndPersists()
    {
        using var context = new BunitContext();
        var analyticsService = new FakeAnalyticsService
        {
            Summary = new AnalyticsDashboardSummary { UniqueVisitors = 1250 }
        };
        var privacyService = new FakePrivacyPreferencesService
        {
            Preferences = new PrivacyPreferences
            {
                AllowEssentialSiteMetrics = true,
                ShareApproximateLocation = true
            },
            SyncResult = new PrivacyPreferenceSaveResult
            {
                Preferences = new PrivacyPreferences
                {
                    AllowEssentialSiteMetrics = true,
                    ShareApproximateLocation = false
                }
            }
        };
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "axl_visitor_id=visitor-123";

        context.Services.AddSingleton<IAnalyticsService>(analyticsService);
        context.Services.AddSingleton<IPrivacyPreferencesService>(privacyService);
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var cut = context.Render<Privacy>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.FindAll("input[type='checkbox']"), Has.Count.EqualTo(4));
        });

        cut.FindAll("input[type='checkbox']")[1].Change(false);
        cut.Find("button.btn-danger").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Approximate location sharing was turned off and previously stored location-related analytics for this browser were removed."));
            Assert.That(cut.Markup, Does.Not.Contain("Confirm Privacy Preference Change"));
        });

        Assert.That(privacyService.SyncApproximateLocationCallCount, Is.EqualTo(1));
        Assert.That(privacyService.SaveCallCount, Is.EqualTo(1));
        Assert.That(privacyService.LastSavedPreferences!.ShareApproximateLocation, Is.False);
        Assert.That(analyticsService.DeletedLocationVisitorIds, Is.EqualTo(["visitor-123"]));
    }

    [Test]
    public void Privacy_WhenApproximateLocationEnableHitsPermissionDenied_ShowsStatusWithoutDeletingData()
    {
        using var context = new BunitContext();
        var analyticsService = new FakeAnalyticsService
        {
            Summary = new AnalyticsDashboardSummary { UniqueVisitors = 1250 }
        };
        var privacyService = new FakePrivacyPreferencesService
        {
            Preferences = new PrivacyPreferences
            {
                AllowEssentialSiteMetrics = true,
                ShareApproximateLocation = false
            },
            SyncResult = new PrivacyPreferenceSaveResult
            {
                Preferences = new PrivacyPreferences
                {
                    AllowEssentialSiteMetrics = true,
                    ShareApproximateLocation = false
                },
                LocationPermissionDenied = true
            },
            SaveResult = new PrivacyPreferenceSaveResult
            {
                Preferences = new PrivacyPreferences
                {
                    AllowEssentialSiteMetrics = true,
                    ShareApproximateLocation = false
                },
                LocationPermissionDenied = true
            }
        };

        context.Services.AddSingleton<IAnalyticsService>(analyticsService);
        context.Services.AddSingleton<IPrivacyPreferencesService>(privacyService);
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });

        var cut = context.Render<Privacy>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.FindAll("input[type='checkbox']"), Has.Count.EqualTo(4));
        });

        cut.FindAll("input[type='checkbox']")[1].Change(true);

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Approximate location sharing stayed off because browser location permission was denied."));
        });

        Assert.That(privacyService.SyncApproximateLocationCallCount, Is.EqualTo(1));
        Assert.That(privacyService.SaveCallCount, Is.EqualTo(1));
        Assert.That(analyticsService.DeletedLocationVisitorIds, Is.Empty);
    }

    [Test]
    public void Privacy_WhenEssentialMetricsIsTurnedOff_OpensConfirmationModal()
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
                ShareApproximateLocation = true
            }
        });
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });

        var cut = context.Render<Privacy>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.FindAll("input[type='checkbox']"), Has.Count.EqualTo(4));
        });

        cut.FindAll("input[type='checkbox']")[0].Change(false);

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Confirm Privacy Preference Change"));
            Assert.That(cut.Markup, Does.Contain("Essential Site Analytics"));
        });
    }

    [Test]
    public void Privacy_WhenEnhancedEngagementIsEnabled_PersistsPreference()
    {
        using var context = new BunitContext();
        var privacyService = new FakePrivacyPreferencesService
        {
            Preferences = new PrivacyPreferences()
        };

        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService
        {
            Summary = new AnalyticsDashboardSummary { UniqueVisitors = 1250 }
        });
        context.Services.AddSingleton<IPrivacyPreferencesService>(privacyService);
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });

        var cut = context.Render<Privacy>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.FindAll("input[type='checkbox']"), Has.Count.EqualTo(4));
        });

        cut.FindAll("input[type='checkbox']")[2].Change(true);

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Privacy preferences updated for this browser."));
        });

        Assert.That(privacyService.SaveCallCount, Is.EqualTo(1));
        Assert.That(privacyService.LastSavedPreferences!.AllowEnhancedEngagementMetrics, Is.True);
    }

    [Test]
    public void Privacy_WhenPersonalizationIsTurnedOff_OpensConfirmationModal()
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
            Assert.That(cut.FindAll("input[type='checkbox']"), Has.Count.EqualTo(4));
        });

        cut.FindAll("input[type='checkbox']")[3].Change(false);

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Confirm Privacy Preference Change"));
            Assert.That(cut.Markup, Does.Contain("Personalization Metrics"));
        });
    }

    private sealed class FakeAnalyticsService : IAnalyticsService
    {
        public AnalyticsDashboardSummary Summary { get; set; } = new();

        public List<string> DeletedLocationVisitorIds { get; } = [];

        public Task RecordPageVisitAsync(PageVisitMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordExternalLinkClickAsync(ExternalLinkClickMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteVisitorDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteVisitorLocationDataAsync(string clientId, CancellationToken cancellationToken = default)
        {
            DeletedLocationVisitorIds.Add(clientId);
            return Task.CompletedTask;
        }

        public Task<AnalyticsDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);

        public Task<VisitorCollectedDataViewModel> GetVisitorCollectedDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(new VisitorCollectedDataViewModel());
    }

    private sealed class FakePrivacyPreferencesService : IPrivacyPreferencesService
    {
        public PrivacyPreferences Preferences { get; set; } = new();

        public PrivacyPreferenceSaveResult? SaveResult { get; set; }

        public PrivacyPreferenceSaveResult? SyncResult { get; set; }

        public int SaveCallCount { get; private set; }

        public int SyncApproximateLocationCallCount { get; private set; }

        public PrivacyPreferences? LastSavedPreferences { get; private set; }

        public Task<PrivacyPreferences> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Preferences);

        public Task<PrivacyPreferenceSaveResult> SaveAsync(PrivacyPreferences preferences, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            LastSavedPreferences = Clone(preferences);
            var result = SaveResult ?? new PrivacyPreferenceSaveResult { Preferences = Clone(preferences) };
            Preferences = Clone(result.Preferences);
            return Task.FromResult(result);
        }

        public Task<PrivacyPreferenceSaveResult> SyncApproximateLocationPreferenceAsync(PrivacyPreferences preferences, CancellationToken cancellationToken = default)
        {
            SyncApproximateLocationCallCount++;
            var result = SyncResult ?? new PrivacyPreferenceSaveResult { Preferences = Clone(preferences) };
            Preferences = Clone(result.Preferences);
            return Task.FromResult(result);
        }

        private static PrivacyPreferences Clone(PrivacyPreferences preferences)
        {
            return new PrivacyPreferences
            {
                AllowEssentialSiteMetrics = preferences.AllowEssentialSiteMetrics,
                ShareApproximateLocation = preferences.ShareApproximateLocation,
                AllowEnhancedEngagementMetrics = preferences.AllowEnhancedEngagementMetrics,
                AllowPersonalizationMetrics = preferences.AllowPersonalizationMetrics
            };
        }
    }
}
