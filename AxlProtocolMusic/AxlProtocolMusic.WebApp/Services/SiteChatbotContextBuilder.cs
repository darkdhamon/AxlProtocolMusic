using System.Text;
using AxlProtocolMusic.WebApp.Services.Interfaces;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class SiteChatbotContextBuilder : ISiteChatbotContextBuilder
{
    private const int MaxDetailedReleaseCount = 8;
    private const int MaxReleaseListCount = 18;
    private const int MaxNewsCount = 8;
    private const int MaxTimelineCount = 10;

    private readonly IAboutPageService _aboutPageService;
    private readonly IReleaseService _releaseService;
    private readonly INewsArticleService _newsArticleService;
    private readonly ITimelineEventService _timelineEventService;

    public SiteChatbotContextBuilder(
        IAboutPageService aboutPageService,
        IReleaseService releaseService,
        INewsArticleService newsArticleService,
        ITimelineEventService timelineEventService)
    {
        _aboutPageService = aboutPageService;
        _releaseService = releaseService;
        _newsArticleService = newsArticleService;
        _timelineEventService = timelineEventService;
    }

    public async Task<string> BuildAsync(CancellationToken cancellationToken = default)
    {
        var aboutTask = _aboutPageService.GetAsync(cancellationToken);
        var releasesTask = _releaseService.GetPagedReleasesAsync(
            searchTerm: null,
            pageNumber: 1,
            pageSize: 250,
            includeUnpublished: false,
            cancellationToken: cancellationToken);
        var newsTask = _newsArticleService.GetArticlesAsync(
            includeUnpublished: false,
            cancellationToken: cancellationToken);
        var timelineTask = _timelineEventService.GetAllAsync(cancellationToken);

        await Task.WhenAll(aboutTask, releasesTask, newsTask, timelineTask);

        var about = await aboutTask;
        var releasePage = await releasesTask;
        var newsArticles = await newsTask;
        var timelineEvents = await timelineTask;

        var builder = new StringBuilder();
        builder.AppendLine("Site navigation:");
        builder.AppendLine("- Home: /");
        builder.AppendLine("- Releases: /releases");
        builder.AppendLine("- News: /news");
        builder.AppendLine("- About Axl Protocol: /about-axl-protocol");
        builder.AppendLine("- Timeline: /timeline");
        builder.AppendLine();

        builder.AppendLine("About Axl Protocol:");
        builder.AppendLine($"- Hero lead: {Truncate(about.HeroLead, 260)}");
        builder.AppendLine($"- Hero body: {Truncate(about.HeroBody, 260)}");

        if (about.FocusPoints.Count > 0)
        {
            builder.AppendLine($"- Focus points: {string.Join(" | ", about.FocusPoints.Take(5))}");
        }

        if (about.NarrativeHighlights.Count > 0)
        {
            builder.AppendLine($"- Narrative highlights: {string.Join(" | ", about.NarrativeHighlights.Take(5))}");
        }

        foreach (var pillar in about.Pillars.Take(4))
        {
            builder.AppendLine($"- Pillar: {pillar.Title} - {Truncate(pillar.Description, 140)}");
        }

        builder.AppendLine();
        builder.AppendLine("Published releases:");

        foreach (var release in releasePage.Items.Take(MaxReleaseListCount))
        {
            builder.AppendLine($"- {release.Title} | {release.ReleaseDateUtc:yyyy-MM-dd} | /releases/{Uri.EscapeDataString(release.Slug)}");
            builder.AppendLine($"  Summary: {Truncate(release.ShortDescription, 180)}");
        }

        builder.AppendLine();
        builder.AppendLine("Detailed release notes:");

        foreach (var release in releasePage.Items.Take(MaxDetailedReleaseCount))
        {
            var details = await _releaseService.GetReleaseBySlugAsync(
                release.Slug,
                includeUnpublished: false,
                cancellationToken: cancellationToken);

            if (details is null)
            {
                continue;
            }

            builder.AppendLine($"- Release: {details.Title}");
            builder.AppendLine($"  Path: /releases/{Uri.EscapeDataString(details.Slug)}");
            builder.AppendLine($"  Type: {details.ReleaseType}");
            builder.AppendLine($"  Tags: {(details.Tags.Count == 0 ? "none" : string.Join(", ", details.Tags.Take(8)))}");

            if (details.Credits.Count > 0)
            {
                var credits = details.Credits
                    .Take(6)
                    .Select(credit => $"{credit.Name} ({string.Join(", ", credit.Roles.Take(3))})");
                builder.AppendLine($"  Credits: {string.Join(" | ", credits)}");
            }

            if (details.Tracks.Count > 0)
            {
                var tracks = details.Tracks
                    .Take(8)
                    .Select(track => string.IsNullOrWhiteSpace(track.Duration)
                        ? track.Title
                        : $"{track.Title} [{track.Duration}]");
                builder.AppendLine($"  Tracks: {string.Join(" | ", tracks)}");
            }

            if (!string.IsNullOrWhiteSpace(details.Story))
            {
                builder.AppendLine($"  Story: {Truncate(details.Story, 260)}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Recent news articles:");

        foreach (var article in newsArticles.Take(MaxNewsCount))
        {
            builder.AppendLine($"- {article.Title} | {article.PublicationDateUtc:yyyy-MM-dd} | /news");
            builder.AppendLine($"  Preview: {Truncate(article.Content, 220)}");
        }

        builder.AppendLine();
        builder.AppendLine("Timeline events:");

        foreach (var timelineEvent in timelineEvents.Take(MaxTimelineCount))
        {
            builder.AppendLine($"- {timelineEvent.Title} | {timelineEvent.EventDateUtc:yyyy-MM-dd} | /timeline");
            builder.AppendLine($"  Type: {timelineEvent.EventType}");
            builder.AppendLine($"  Summary: {Truncate(timelineEvent.ShortDescription, 180)}");
        }

        return builder.ToString().Trim();
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..maxLength].TrimEnd()}...";
    }
}
