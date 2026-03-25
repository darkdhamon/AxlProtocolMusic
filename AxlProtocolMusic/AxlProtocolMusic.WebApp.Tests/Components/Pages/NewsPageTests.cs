using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class NewsPageTests
{
    [Test]
    public void News_WhenPublicArticlesExist_RendersFeaturedAndFeedContent()
    {
        using var context = CreateContext(out var newsService);
        context.AddAuthorization().SetNotAuthorized();
        newsService.Articles =
        [
            new NewsArticle
            {
                Id = "featured-1",
                Title = "Launch Story",
                Slug = "launch-story",
                Content = "This is the featured article content for the launch story.",
                ImageUrl = "https://cdn.example/launch.jpg",
                PublicationDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
                IsPublished = true,
                IsFeatured = true
            },
            new NewsArticle
            {
                Id = "feed-1",
                Title = "Studio Update",
                Slug = "studio-update",
                Content = "A shorter production update for the news feed.",
                ImageUrl = string.Empty,
                PublicationDateUtc = DateTimeOffset.UtcNow.AddDays(-2),
                IsPublished = true,
                IsFeatured = false
            },
            new NewsArticle
            {
                Id = "draft-1",
                Title = "Draft Story",
                Slug = "draft-story",
                Content = "This should stay hidden from public visitors.",
                ImageUrl = string.Empty,
                PublicationDateUtc = DateTimeOffset.UtcNow.AddDays(1),
                IsPublished = false,
                IsFeatured = false
            }
        ];

        var cut = context.Render<News>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("News Articles"));
            Assert.That(cut.Markup, Does.Contain("Launch Story"));
            Assert.That(cut.Markup, Does.Contain("Studio Update"));
            Assert.That(cut.Markup, Does.Not.Contain("Draft Story"));
            Assert.That(cut.Markup, Does.Contain("2 visible articles"));
            Assert.That(cut.Markup, Does.Contain("that are published and live."));
            Assert.That(cut.Markup, Does.Contain("Read More"));
            Assert.That(cut.Markup, Does.Contain("All visible articles are loaded."));
        });

        Assert.That(newsService.LastIncludeUnpublished, Is.True);
    }

    [Test]
    public void News_WhenArticleQueryParameterIsProvided_OpensMatchingArticle()
    {
        using var context = CreateContext(out var newsService);
        context.AddAuthorization().SetNotAuthorized();
        var navigation = context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        newsService.Articles =
        [
            new NewsArticle
            {
                Id = "article-1",
                Title = "Launch Story",
                Slug = "launch-story",
                Content = "This is the featured article content for the launch story.",
                ImageUrl = "https://cdn.example/launch.jpg",
                PublicationDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
                IsPublished = true,
                IsFeatured = false
            }
        ];

        navigation.NavigateTo("/news?article=launch-story");

        var cut = context.Render<News>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("News Article"));
            Assert.That(cut.Markup, Does.Contain("Launch Story"));
        });
    }

    private static BunitContext CreateContext(out FakeNewsArticleService newsService)
    {
        var context = new BunitContext();
        newsService = new FakeNewsArticleService();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton<INewsArticleService>(newsService);
        context.Services.AddSingleton<IImageStorageService, FakeImageStorageService>();
        return context;
    }

    private sealed class FakeNewsArticleService : INewsArticleService
    {
        public IReadOnlyList<NewsArticle> Articles { get; set; } = [];

        public bool LastIncludeUnpublished { get; private set; }

        public Task<IReadOnlyList<NewsArticle>> GetArticlesAsync(bool includeUnpublished = false, CancellationToken cancellationToken = default)
        {
            LastIncludeUnpublished = includeUnpublished;
            return Task.FromResult(Articles);
        }

        public Task<NewsArticle> CreateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new NewsArticle());

        public Task<NewsArticle> UpdateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new NewsArticle());

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public bool IsManagedImageUrl(string? imageUrl) => false;
    }

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public Task<ImageSaveResult> SaveReleaseImageAsync(IFormFile file, CancellationToken cancellationToken = default)
            => Task.FromResult(new ImageSaveResult());

        public bool IsManagedImageUrl(string? imageUrl) => false;

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
