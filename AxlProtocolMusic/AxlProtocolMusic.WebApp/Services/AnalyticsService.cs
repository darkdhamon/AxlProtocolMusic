using AxlProtocolMusic.WebApp.Models.Analytics;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Interfaces;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly IRepository<PageVisitMetric> _pageVisitRepository;
    private readonly IRepository<ExternalLinkClickMetric> _externalLinkRepository;

    public AnalyticsService(
        IRepository<PageVisitMetric> pageVisitRepository,
        IRepository<ExternalLinkClickMetric> externalLinkRepository)
    {
        _pageVisitRepository = pageVisitRepository;
        _externalLinkRepository = externalLinkRepository;
    }

    public Task RecordPageVisitAsync(PageVisitMetric metric, CancellationToken cancellationToken = default)
    {
        metric.Id = Guid.NewGuid().ToString("N");
        return _pageVisitRepository.CreateAsync(metric, cancellationToken);
    }

    public Task RecordExternalLinkClickAsync(ExternalLinkClickMetric metric, CancellationToken cancellationToken = default)
    {
        metric.Id = Guid.NewGuid().ToString("N");
        return _externalLinkRepository.CreateAsync(metric, cancellationToken);
    }

    public async Task DeleteVisitorDataAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        var pageVisits = await _pageVisitRepository.FindAsync(metric => metric.ClientId == clientId, cancellationToken);
        foreach (var pageVisit in pageVisits)
        {
            await _pageVisitRepository.DeleteAsync(pageVisit.Id, cancellationToken);
        }

        var externalClicks = await _externalLinkRepository.FindAsync(metric => metric.ClientId == clientId, cancellationToken);
        foreach (var externalClick in externalClicks)
        {
            await _externalLinkRepository.DeleteAsync(externalClick.Id, cancellationToken);
        }
    }

    public async Task DeleteVisitorLocationDataAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        var pageVisits = await _pageVisitRepository.FindAsync(metric => metric.ClientId == clientId, cancellationToken);
        foreach (var pageVisit in pageVisits.Where(HasLocationData))
        {
            pageVisit.Region = "Unknown";
            pageVisit.ApproximateLatitude = null;
            pageVisit.ApproximateLongitude = null;
            await _pageVisitRepository.UpdateAsync(pageVisit, cancellationToken);
        }

        var externalClicks = await _externalLinkRepository.FindAsync(metric => metric.ClientId == clientId, cancellationToken);
        foreach (var externalClick in externalClicks.Where(HasLocationData))
        {
            externalClick.Region = "Unknown";
            externalClick.ApproximateLatitude = null;
            externalClick.ApproximateLongitude = null;
            await _externalLinkRepository.UpdateAsync(externalClick, cancellationToken);
        }
    }

    public async Task<AnalyticsDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var visits = (await _pageVisitRepository.GetAllAsync(cancellationToken))
            .Where(metric => metric.VisitedAtUtc >= cutoff)
            .ToList();
        var externalClicks = (await _externalLinkRepository.GetAllAsync(cancellationToken))
            .Where(metric => metric.ClickedAtUtc >= cutoff)
            .ToList();

        var topPages = visits
            .GroupBy(metric => new { metric.PagePath, metric.PageTitle })
            .Select(group => new PageVisitAggregate
            {
                PagePath = group.Key.PagePath,
                PageTitle = group.Key.PageTitle,
                VisitCount = group.Count(),
                AverageDurationSeconds = group.Average(metric => metric.DurationSeconds)
            })
            .OrderByDescending(group => group.VisitCount)
            .ThenBy(group => group.PagePath)
            .Take(5)
            .ToList();

        var topRegions = visits
            .GroupBy(metric => string.IsNullOrWhiteSpace(metric.Region) ? "Unknown" : metric.Region)
            .Select(group => new RegionVisitAggregate
            {
                Region = group.Key,
                VisitCount = group.Count()
            })
            .OrderByDescending(group => group.VisitCount)
            .ThenBy(group => group.Region)
            .Take(5)
            .ToList();

        var uniqueVisitors = visits
            .Select(metric => metric.ClientId)
            .Where(clientId => !string.IsNullOrWhiteSpace(clientId))
            .Distinct(StringComparer.Ordinal)
            .Count();

        var repeatVisitors = visits
            .Where(metric => !string.IsNullOrWhiteSpace(metric.ClientId))
            .GroupBy(metric => metric.ClientId, StringComparer.Ordinal)
            .Count(group => group.Count() > 1);

        var topExternalLinks = externalClicks
            .GroupBy(metric => new { metric.DestinationUrl, metric.LinkLabel, metric.SourcePagePath })
            .Select(group => new ExternalLinkClickAggregate
            {
                DestinationUrl = group.Key.DestinationUrl,
                LinkLabel = group.Key.LinkLabel,
                SourcePagePath = group.Key.SourcePagePath,
                ClickCount = group.Count()
            })
            .OrderByDescending(group => group.ClickCount)
            .ThenBy(group => group.LinkLabel)
            .Take(8)
            .ToList();

        return new AnalyticsDashboardSummary
        {
            TopPages = topPages,
            TopRegions = topRegions,
            TopExternalLinks = topExternalLinks,
            TotalVisits = visits.Count,
            UniqueVisitors = uniqueVisitors,
            RepeatVisitors = repeatVisitors,
            AverageDurationSeconds = visits.Count == 0 ? 0 : visits.Average(metric => metric.DurationSeconds)
        };
    }

    public async Task<VisitorCollectedDataViewModel> GetVisitorCollectedDataAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return new VisitorCollectedDataViewModel();
        }

        var pageVisits = await _pageVisitRepository.FindAsync(metric => metric.ClientId == clientId, cancellationToken);
        var externalClicks = await _externalLinkRepository.FindAsync(metric => metric.ClientId == clientId, cancellationToken);

        return new VisitorCollectedDataViewModel
        {
            VisitorId = clientId,
            PageVisits = pageVisits
                .OrderByDescending(metric => metric.VisitedAtUtc)
                .Select(metric => new VisitorPageVisitViewModel
                {
                    PagePath = metric.PagePath,
                    PageTitle = metric.PageTitle,
                    DurationSeconds = metric.DurationSeconds,
                    VisitedAtUtc = metric.VisitedAtUtc,
                    Region = metric.Region,
                    ApproximateLatitude = metric.ApproximateLatitude,
                    ApproximateLongitude = metric.ApproximateLongitude,
                    ReferrerPath = metric.ReferrerPath
                })
                .ToList(),
            ExternalLinkClicks = externalClicks
                .OrderByDescending(metric => metric.ClickedAtUtc)
                .Select(metric => new VisitorExternalLinkClickViewModel
                {
                    SourcePagePath = metric.SourcePagePath,
                    DestinationUrl = metric.DestinationUrl,
                    LinkLabel = metric.LinkLabel,
                    ClickedAtUtc = metric.ClickedAtUtc,
                    Region = metric.Region,
                    ApproximateLatitude = metric.ApproximateLatitude,
                    ApproximateLongitude = metric.ApproximateLongitude
                })
                .ToList()
        };
    }

    private static bool HasLocationData(PageVisitMetric metric)
    {
        return !string.IsNullOrWhiteSpace(metric.Region)
            && !string.Equals(metric.Region, "Unknown", StringComparison.OrdinalIgnoreCase)
            || metric.ApproximateLatitude.HasValue
            || metric.ApproximateLongitude.HasValue;
    }

    private static bool HasLocationData(ExternalLinkClickMetric metric)
    {
        return !string.IsNullOrWhiteSpace(metric.Region)
            && !string.Equals(metric.Region, "Unknown", StringComparison.OrdinalIgnoreCase)
            || metric.ApproximateLatitude.HasValue
            || metric.ApproximateLongitude.HasValue;
    }
}
