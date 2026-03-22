using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;

namespace AxlProtocolMusic.WebApp.Services.Identity;

public sealed class NewsArticleSeedService
{
    private static readonly string[] SeedImageUrls =
    [
        string.Empty,
        "/uploads/releases/24ecbe52da3f44f9971386b6aa824d39.png",
        "/uploads/releases/2be94a351134427cbc7d246b2466f5fd.png",
        "/uploads/releases/6ad4a3897ec144b3bb4970999daca630.png",
        "/uploads/releases/6af29a343cbd4d1db7cb7c378f345558.png",
        "/uploads/releases/81cbbdd5332d47418a14261319b98625.png",
        "/uploads/releases/ba890855935c4e9b97ffc347ca7aea49.png",
        "/uploads/releases/deea5e23c13d462baebeedd2708386a4.png",
        "/uploads/releases/e8ab31b5f4904929b4ac7f899706ab59.png",
        "/Assets/Logo/Site%20Logo.png"
    ];

    private readonly IRepository<NewsArticle> _newsArticleRepository;

    public NewsArticleSeedService(IRepository<NewsArticle> newsArticleRepository)
    {
        _newsArticleRepository = newsArticleRepository;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingArticles = await _newsArticleRepository.GetAllAsync(cancellationToken);
        if (existingArticles.Count > 0)
        {
            return;
        }

        foreach (var article in BuildSeedArticles())
        {
            await _newsArticleRepository.CreateAsync(article, cancellationToken);
        }
    }

    private static IReadOnlyList<NewsArticle> BuildSeedArticles()
    {
        var articles = new List<NewsArticle>(50);
        var now = DateTimeOffset.UtcNow;
        var categories =
            new[]
            {
                "Release Update",
                "Studio Journal",
                "Creative Process",
                "Milestone Report",
                "Catalog Note",
                "Behind The Scenes"
            };
        var headlines =
            new[]
            {
                "Shaping The Next Chapter",
                "Refining The Sound",
                "Building Momentum Around The Catalog",
                "Turning Sessions Into Story",
                "Mapping The Release Window",
                "Connecting Music With Narrative"
            };
        var focusPoints =
            new[]
            {
                "the release schedule",
                "the visual identity",
                "the latest studio sessions",
                "the growing archive",
                "the story around each track",
                "how new milestones connect to older eras"
            };

        for (var index = 0; index < 50; index++)
        {
            var articleNumber = index + 1;
            var isScheduled = articleNumber % 10 == 0;
            var publicationDateUtc = isScheduled
                ? now.AddDays((articleNumber / 10) + 2)
                : now.AddDays(-(articleNumber - 1) * 2);

            var isPublished = !isScheduled || articleNumber % 20 == 0;
            var title = $"{categories[index % categories.Length]} {articleNumber}: {headlines[index % headlines.Length]}";
            var imageUrl = articleNumber % 4 == 0
                ? string.Empty
                : SeedImageUrls[(index % (SeedImageUrls.Length - 1)) + 1];

            articles.Add(new NewsArticle
            {
                Id = $"seed-news-{articleNumber:00}",
                Title = title,
                Slug = $"seed-news-{articleNumber:00}",
                Content = BuildSeedContent(articleNumber, focusPoints[index % focusPoints.Length], title),
                ImageUrl = imageUrl,
                PublicationDateUtc = publicationDateUtc,
                IsPublished = isPublished,
                IsFeatured = articleNumber <= 5
            });
        }

        return articles
            .OrderByDescending(article => article.PublicationDateUtc)
            .ToList();
    }

    private static string BuildSeedContent(int articleNumber, string focusPoint, string title)
    {
        return string.Join(
            "\n\n",
            [
                $"{title} is part of the News Articles seed set and exists to preview how long-form updates can read once the editorial system is wired into the site. This sample article follows the same model we discussed, with a title, publication date, optional image support, and enough body copy to make the card preview and modal feel realistic.",
                $"In this sample story, the update focuses on {focusPoint}. The goal is to show how an article can frame context around a release, describe what changed in the creative process, and give visitors a stronger sense of momentum across the broader Axl Protocol Music project without forcing them to leave the news feed immediately.",
                $"Article {articleNumber} also demonstrates the reading flow for the modal experience. A visitor sees the first fifty words on the card, notices the trailing ellipsis when more text exists, clicks Read More, and then lands in a larger article presentation with the hero image at the top and the full story continuing below it in a clean, readable layout.",
                $"This dummy copy is intentionally straightforward, but it points toward the structure we can use later for release announcements, studio journals, premiere notes, milestone recaps, or reflections that connect a new post to the timeline and catalog. Once the real backend is added, these placeholders can give way to actual published stories without changing the overall page behavior."
            ]);
    }
}
