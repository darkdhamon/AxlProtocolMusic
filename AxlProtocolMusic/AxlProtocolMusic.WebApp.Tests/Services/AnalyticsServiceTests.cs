using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Models;
using AxlProtocolMusic.WebApp.Models.Analytics;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class AnalyticsServiceTests
{
    [Test]
    public async Task RecordPageVisitAsync_AssignsIdAndPersistsMetric()
    {
        var pageVisitRepository = new InMemoryRepository<PageVisitMetric>([]);
        var externalClickRepository = new InMemoryRepository<ExternalLinkClickMetric>([]);
        var service = new AnalyticsService(pageVisitRepository, externalClickRepository);

        var metric = new PageVisitMetric
        {
            PagePath = "/releases",
            PageTitle = "Releases",
            DurationSeconds = 15
        };

        await service.RecordPageVisitAsync(metric);

        Assert.That(metric.Id, Is.Not.Empty);
        Assert.That(pageVisitRepository.CreatedDocuments, Has.Count.EqualTo(1));
        Assert.That(pageVisitRepository.CreatedDocuments.Single(), Is.SameAs(metric));
    }

    [Test]
    public async Task RecordExternalLinkClickAsync_AssignsIdAndPersistsMetric()
    {
        var pageVisitRepository = new InMemoryRepository<PageVisitMetric>([]);
        var externalClickRepository = new InMemoryRepository<ExternalLinkClickMetric>([]);
        var service = new AnalyticsService(pageVisitRepository, externalClickRepository);

        var metric = new ExternalLinkClickMetric
        {
            SourcePagePath = "/releases",
            DestinationUrl = "https://example.test",
            LinkLabel = "Spotify"
        };

        await service.RecordExternalLinkClickAsync(metric);

        Assert.That(metric.Id, Is.Not.Empty);
        Assert.That(externalClickRepository.CreatedDocuments, Has.Count.EqualTo(1));
        Assert.That(externalClickRepository.CreatedDocuments.Single(), Is.SameAs(metric));
    }

    [Test]
    public async Task DeleteVisitorDataAsync_WhenClientIdIsBlank_DoesNothing()
    {
        var pageVisitRepository = new InMemoryRepository<PageVisitMetric>([]);
        var externalClickRepository = new InMemoryRepository<ExternalLinkClickMetric>([]);
        var service = new AnalyticsService(pageVisitRepository, externalClickRepository);

        await service.DeleteVisitorDataAsync(" ");

        Assert.That(pageVisitRepository.FindCallCount, Is.EqualTo(0));
        Assert.That(externalClickRepository.FindCallCount, Is.EqualTo(0));
        Assert.That(pageVisitRepository.DeletedIds, Is.Empty);
        Assert.That(externalClickRepository.DeletedIds, Is.Empty);
    }

    [Test]
    public async Task DeleteVisitorDataAsync_DeletesMatchingPageVisitsAndExternalClicks()
    {
        var pageVisitRepository = new InMemoryRepository<PageVisitMetric>(
        [
            new PageVisitMetric { Id = "visit-1", ClientId = "visitor-1" },
            new PageVisitMetric { Id = "visit-2", ClientId = "visitor-1" },
            new PageVisitMetric { Id = "visit-3", ClientId = "visitor-2" }
        ]);

        var externalClickRepository = new InMemoryRepository<ExternalLinkClickMetric>(
        [
            new ExternalLinkClickMetric { Id = "click-1", ClientId = "visitor-1" },
            new ExternalLinkClickMetric { Id = "click-2", ClientId = "visitor-2" }
        ]);

        var service = new AnalyticsService(pageVisitRepository, externalClickRepository);

        await service.DeleteVisitorDataAsync("visitor-1");

        Assert.That(pageVisitRepository.DeletedIds, Is.EqualTo(new[] { "visit-1", "visit-2" }));
        Assert.That(externalClickRepository.DeletedIds, Is.EqualTo(new[] { "click-1" }));
        Assert.That(pageVisitRepository.Documents.Select(item => item.Id), Is.EqualTo(new[] { "visit-3" }));
        Assert.That(externalClickRepository.Documents.Select(item => item.Id), Is.EqualTo(new[] { "click-2" }));
    }

    [Test]
    public async Task DeleteVisitorLocationDataAsync_WhenClientIdIsBlank_DoesNothing()
    {
        var pageVisitRepository = new InMemoryRepository<PageVisitMetric>([]);
        var externalClickRepository = new InMemoryRepository<ExternalLinkClickMetric>([]);
        var service = new AnalyticsService(pageVisitRepository, externalClickRepository);

        await service.DeleteVisitorLocationDataAsync("");

        Assert.That(pageVisitRepository.FindCallCount, Is.EqualTo(0));
        Assert.That(externalClickRepository.FindCallCount, Is.EqualTo(0));
        Assert.That(pageVisitRepository.UpdatedDocuments, Is.Empty);
        Assert.That(externalClickRepository.UpdatedDocuments, Is.Empty);
    }

    [Test]
    public async Task DeleteVisitorLocationDataAsync_ScrubsOnlyRecordsWithLocationData()
    {
        var pageVisitWithRegion = new PageVisitMetric
        {
            Id = "visit-1",
            ClientId = "visitor-1",
            Region = "Austin, TX",
            ApproximateLatitude = 30.2672,
            ApproximateLongitude = -97.7431
        };

        var pageVisitWithoutLocation = new PageVisitMetric
        {
            Id = "visit-2",
            ClientId = "visitor-1",
            Region = "Unknown"
        };

        var pageVisitWithCoordinatesOnly = new PageVisitMetric
        {
            Id = "visit-3",
            ClientId = "visitor-1",
            Region = string.Empty,
            ApproximateLatitude = 10
        };

        var externalClickWithRegion = new ExternalLinkClickMetric
        {
            Id = "click-1",
            ClientId = "visitor-1",
            Region = "Chicago, IL"
        };

        var externalClickWithoutLocation = new ExternalLinkClickMetric
        {
            Id = "click-2",
            ClientId = "visitor-1",
            Region = "Unknown"
        };

        var pageVisitRepository = new InMemoryRepository<PageVisitMetric>(
        [
            pageVisitWithRegion,
            pageVisitWithoutLocation,
            pageVisitWithCoordinatesOnly
        ]);

        var externalClickRepository = new InMemoryRepository<ExternalLinkClickMetric>(
        [
            externalClickWithRegion,
            externalClickWithoutLocation
        ]);

        var service = new AnalyticsService(pageVisitRepository, externalClickRepository);

        await service.DeleteVisitorLocationDataAsync("visitor-1");

        Assert.That(pageVisitRepository.UpdatedDocuments.Select(item => item.Id), Is.EqualTo(new[] { "visit-1", "visit-3" }));
        Assert.That(externalClickRepository.UpdatedDocuments.Select(item => item.Id), Is.EqualTo(new[] { "click-1" }));

        Assert.Multiple(() =>
        {
            Assert.That(pageVisitWithRegion.Region, Is.EqualTo("Unknown"));
            Assert.That(pageVisitWithRegion.ApproximateLatitude, Is.Null);
            Assert.That(pageVisitWithRegion.ApproximateLongitude, Is.Null);

            Assert.That(pageVisitWithCoordinatesOnly.Region, Is.EqualTo("Unknown"));
            Assert.That(pageVisitWithCoordinatesOnly.ApproximateLatitude, Is.Null);
            Assert.That(pageVisitWithCoordinatesOnly.ApproximateLongitude, Is.Null);

            Assert.That(pageVisitWithoutLocation.Region, Is.EqualTo("Unknown"));
            Assert.That(externalClickWithRegion.Region, Is.EqualTo("Unknown"));
            Assert.That(externalClickWithRegion.ApproximateLatitude, Is.Null);
            Assert.That(externalClickWithRegion.ApproximateLongitude, Is.Null);
        });
    }

    [Test]
    public async Task GetDashboardSummaryAsync_AggregatesRecentMetricsAndIgnoresOlderData()
    {
        var now = DateTimeOffset.UtcNow;
        var pageVisitRepository = new InMemoryRepository<PageVisitMetric>(
        [
            new PageVisitMetric { Id = "1", PagePath = "/releases", PageTitle = "Releases", DurationSeconds = 10, VisitedAtUtc = now.AddDays(-1), ClientId = "alpha", Region = "Texas" },
            new PageVisitMetric { Id = "2", PagePath = "/releases", PageTitle = "Releases", DurationSeconds = 20, VisitedAtUtc = now.AddDays(-2), ClientId = "alpha", Region = "Texas" },
            new PageVisitMetric { Id = "3", PagePath = "/news", PageTitle = "News", DurationSeconds = 30, VisitedAtUtc = now.AddDays(-3), ClientId = "beta", Region = "Illinois" },
            new PageVisitMetric { Id = "4", PagePath = "/timeline", PageTitle = "Timeline", DurationSeconds = 40, VisitedAtUtc = now.AddDays(-4), ClientId = "gamma", Region = "" },
            new PageVisitMetric { Id = "5", PagePath = "/about-axl-protocol", PageTitle = "About", DurationSeconds = 50, VisitedAtUtc = now.AddDays(-5), ClientId = "delta", Region = "Texas" },
            new PageVisitMetric { Id = "6", PagePath = "/", PageTitle = "Home", DurationSeconds = 60, VisitedAtUtc = now.AddDays(-6), ClientId = "epsilon", Region = "Oklahoma" },
            new PageVisitMetric { Id = "7", PagePath = "/admin", PageTitle = "Admin", DurationSeconds = 70, VisitedAtUtc = now.AddDays(-7), ClientId = "zeta", Region = "Texas" },
            new PageVisitMetric { Id = "8", PagePath = "/old", PageTitle = "Old", DurationSeconds = 999, VisitedAtUtc = now.AddDays(-31), ClientId = "old", Region = "Nevada" }
        ]);

        var externalClickRepository = new InMemoryRepository<ExternalLinkClickMetric>(
        [
            new ExternalLinkClickMetric { Id = "c1", DestinationUrl = "https://spotify.test", LinkLabel = "Spotify", SourcePagePath = "/releases", ClickedAtUtc = now.AddDays(-1), ClientId = "alpha" },
            new ExternalLinkClickMetric { Id = "c2", DestinationUrl = "https://spotify.test", LinkLabel = "Spotify", SourcePagePath = "/releases", ClickedAtUtc = now.AddDays(-2), ClientId = "beta" },
            new ExternalLinkClickMetric { Id = "c3", DestinationUrl = "https://apple.test", LinkLabel = "Apple Music", SourcePagePath = "/releases", ClickedAtUtc = now.AddDays(-3), ClientId = "gamma" },
            new ExternalLinkClickMetric { Id = "c4", DestinationUrl = "https://bandcamp.test", LinkLabel = "Bandcamp", SourcePagePath = "/news", ClickedAtUtc = now.AddDays(-4), ClientId = "delta" },
            new ExternalLinkClickMetric { Id = "c5", DestinationUrl = "https://youtube.test", LinkLabel = "YouTube", SourcePagePath = "/timeline", ClickedAtUtc = now.AddDays(-5), ClientId = "epsilon" },
            new ExternalLinkClickMetric { Id = "c6", DestinationUrl = "https://archive.test", LinkLabel = "Archive", SourcePagePath = "/old", ClickedAtUtc = now.AddDays(-31), ClientId = "old" }
        ]);

        var service = new AnalyticsService(pageVisitRepository, externalClickRepository);

        var summary = await service.GetDashboardSummaryAsync();

        Assert.That(summary.TotalVisits, Is.EqualTo(7));
        Assert.That(summary.UniqueVisitors, Is.EqualTo(6));
        Assert.That(summary.RepeatVisitors, Is.EqualTo(1));
        Assert.That(summary.AverageDurationSeconds, Is.EqualTo(40));
        Assert.That(summary.TopPages.Select(item => item.PagePath), Is.EqualTo(new[] { "/releases", "/", "/about-axl-protocol", "/admin", "/news" }));
        Assert.That(summary.TopPages[0].VisitCount, Is.EqualTo(2));
        Assert.That(summary.TopPages[0].AverageDurationSeconds, Is.EqualTo(15));
        Assert.That(summary.TopRegions.Select(item => item.Region), Is.EqualTo(new[] { "Texas", "Illinois", "Oklahoma", "Unknown" }));
        Assert.That(summary.TopRegions.Select(item => item.VisitCount), Is.EqualTo(new[] { 4, 1, 1, 1 }));
        Assert.That(summary.TopExternalLinks.Select(item => item.LinkLabel), Is.EqualTo(new[] { "Spotify", "Apple Music", "Bandcamp", "YouTube" }));
        Assert.That(summary.TopExternalLinks[0].ClickCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetDashboardSummaryAsync_WhenNoRecentVisits_ReturnsEmptySummary()
    {
        var now = DateTimeOffset.UtcNow;
        var pageVisitRepository = new InMemoryRepository<PageVisitMetric>(
        [
            new PageVisitMetric { Id = "old-visit", VisitedAtUtc = now.AddDays(-45), DurationSeconds = 15, ClientId = "visitor" }
        ]);

        var externalClickRepository = new InMemoryRepository<ExternalLinkClickMetric>(
        [
            new ExternalLinkClickMetric { Id = "old-click", ClickedAtUtc = now.AddDays(-45), ClientId = "visitor" }
        ]);

        var service = new AnalyticsService(pageVisitRepository, externalClickRepository);

        var summary = await service.GetDashboardSummaryAsync();

        Assert.That(summary.TotalVisits, Is.EqualTo(0));
        Assert.That(summary.UniqueVisitors, Is.EqualTo(0));
        Assert.That(summary.RepeatVisitors, Is.EqualTo(0));
        Assert.That(summary.AverageDurationSeconds, Is.EqualTo(0));
        Assert.That(summary.TopPages, Is.Empty);
        Assert.That(summary.TopRegions, Is.Empty);
        Assert.That(summary.TopExternalLinks, Is.Empty);
    }

    [Test]
    public async Task GetVisitorCollectedDataAsync_WhenClientIdIsBlank_ReturnsEmptyViewModel()
    {
        var pageVisitRepository = new InMemoryRepository<PageVisitMetric>([]);
        var externalClickRepository = new InMemoryRepository<ExternalLinkClickMetric>([]);
        var service = new AnalyticsService(pageVisitRepository, externalClickRepository);

        var result = await service.GetVisitorCollectedDataAsync(" ");

        Assert.That(result.VisitorId, Is.Empty);
        Assert.That(result.HasVisitorId, Is.False);
        Assert.That(result.PageVisits, Is.Empty);
        Assert.That(result.ExternalLinkClicks, Is.Empty);
    }

    [Test]
    public async Task GetVisitorCollectedDataAsync_ReturnsDescendingMappedMetricsForVisitor()
    {
        var pageVisitRepository = new InMemoryRepository<PageVisitMetric>(
        [
            new PageVisitMetric
            {
                Id = "visit-1",
                ClientId = "visitor-1",
                PagePath = "/news",
                PageTitle = "News",
                DurationSeconds = 12,
                VisitedAtUtc = new DateTimeOffset(2026, 3, 24, 12, 0, 0, TimeSpan.Zero),
                Region = "Texas",
                ApproximateLatitude = 30.2,
                ApproximateLongitude = -97.7,
                ReferrerPath = "/"
            },
            new PageVisitMetric
            {
                Id = "visit-2",
                ClientId = "visitor-1",
                PagePath = "/releases",
                PageTitle = "Releases",
                DurationSeconds = 24,
                VisitedAtUtc = new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero),
                Region = "Illinois",
                ReferrerPath = "/news"
            },
            new PageVisitMetric
            {
                Id = "visit-3",
                ClientId = "visitor-2",
                PagePath = "/about-axl-protocol",
                PageTitle = "About",
                DurationSeconds = 99,
                VisitedAtUtc = new DateTimeOffset(2026, 3, 26, 12, 0, 0, TimeSpan.Zero)
            }
        ]);

        var externalClickRepository = new InMemoryRepository<ExternalLinkClickMetric>(
        [
            new ExternalLinkClickMetric
            {
                Id = "click-1",
                ClientId = "visitor-1",
                SourcePagePath = "/releases",
                DestinationUrl = "https://spotify.test",
                LinkLabel = "Spotify",
                ClickedAtUtc = new DateTimeOffset(2026, 3, 24, 10, 0, 0, TimeSpan.Zero),
                Region = "Texas",
                ApproximateLatitude = 30.2,
                ApproximateLongitude = -97.7
            },
            new ExternalLinkClickMetric
            {
                Id = "click-2",
                ClientId = "visitor-1",
                SourcePagePath = "/news",
                DestinationUrl = "https://apple.test",
                LinkLabel = "Apple Music",
                ClickedAtUtc = new DateTimeOffset(2026, 3, 25, 10, 0, 0, TimeSpan.Zero),
                Region = "Illinois"
            },
            new ExternalLinkClickMetric
            {
                Id = "click-3",
                ClientId = "visitor-2",
                SourcePagePath = "/about",
                DestinationUrl = "https://example.test",
                LinkLabel = "Other",
                ClickedAtUtc = new DateTimeOffset(2026, 3, 26, 10, 0, 0, TimeSpan.Zero)
            }
        ]);

        var service = new AnalyticsService(pageVisitRepository, externalClickRepository);

        var result = await service.GetVisitorCollectedDataAsync("visitor-1");

        Assert.That(result.VisitorId, Is.EqualTo("visitor-1"));
        Assert.That(result.HasVisitorId, Is.True);
        Assert.That(result.PageVisits.Select(item => item.PagePath), Is.EqualTo(new[] { "/releases", "/news" }));
        Assert.That(result.PageVisits[0].PageTitle, Is.EqualTo("Releases"));
        Assert.That(result.PageVisits[0].ReferrerPath, Is.EqualTo("/news"));
        Assert.That(result.PageVisits[1].ApproximateLatitude, Is.EqualTo(30.2));
        Assert.That(result.PageVisits[1].ApproximateLongitude, Is.EqualTo(-97.7));
        Assert.That(result.ExternalLinkClicks.Select(item => item.LinkLabel), Is.EqualTo(new[] { "Apple Music", "Spotify" }));
        Assert.That(result.ExternalLinkClicks[0].DestinationUrl, Is.EqualTo("https://apple.test"));
        Assert.That(result.ExternalLinkClicks[1].SourcePagePath, Is.EqualTo("/releases"));
        Assert.That(result.ExternalLinkClicks[1].ApproximateLatitude, Is.EqualTo(30.2));
    }

    private sealed class InMemoryRepository<TDocument> : IRepository<TDocument>
        where TDocument : class, IEntity
    {
        public InMemoryRepository(IEnumerable<TDocument> documents)
        {
            Documents = documents.ToList();
        }

        public List<TDocument> Documents { get; }

        public List<TDocument> CreatedDocuments { get; } = [];

        public List<TDocument> UpdatedDocuments { get; } = [];

        public List<string> DeletedIds { get; } = [];

        public int FindCallCount { get; private set; }

        public Task CreateAsync(TDocument document, CancellationToken cancellationToken = default)
        {
            CreatedDocuments.Add(document);
            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            DeletedIds.Add(id);
            Documents.RemoveAll(document => string.Equals(document.Id, id, StringComparison.Ordinal));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TDocument>> FindAsync(Expression<Func<TDocument, bool>> filter, CancellationToken cancellationToken = default)
        {
            FindCallCount++;
            var predicate = filter.Compile();
            return Task.FromResult<IReadOnlyList<TDocument>>(Documents.Where(predicate).ToList());
        }

        public Task<IReadOnlyList<TDocument>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TDocument>>(Documents.ToList());

        public Task<TDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Documents.FirstOrDefault(document => string.Equals(document.Id, id, StringComparison.Ordinal)));

        public Task UpdateAsync(TDocument document, CancellationToken cancellationToken = default)
        {
            UpdatedDocuments.Add(document);
            return Task.CompletedTask;
        }
    }
}
