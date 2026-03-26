using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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

    [Test]
    public void News_WhenAdminViewsPage_IncludesUnpublishedArticlesAndAdminActions()
    {
        using var context = CreateContext(out var newsService);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        newsService.Articles =
        [
            new NewsArticle
            {
                Id = "draft-1",
                Title = "Draft Story",
                Slug = "draft-story",
                Content = "Private preview content for admins.",
                PublicationDateUtc = DateTimeOffset.UtcNow.AddDays(2),
                IsPublished = false,
                IsFeatured = false
            }
        ];

        var cut = context.Render<News>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Draft Story"));
            Assert.That(cut.Markup, Does.Contain("Admin Preview"));
            Assert.That(cut.Markup, Does.Contain("including admin-only scheduled or unpublished entries."));
            Assert.That(cut.Markup, Does.Contain("Edit"));
            Assert.That(cut.Markup, Does.Contain("Delete"));
        });

        Assert.That(newsService.LastIncludeUnpublished, Is.True);
    }

    [Test]
    public void News_WhenArrowKeysArePressed_CyclesFeaturedArticles()
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
                PublicationDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
                IsPublished = true,
                IsFeatured = true
            },
            new NewsArticle
            {
                Id = "featured-2",
                Title = "Studio Journal",
                Slug = "studio-journal",
                Content = "This is the featured article content for the studio journal.",
                PublicationDateUtc = DateTimeOffset.UtcNow.AddDays(-2),
                IsPublished = true,
                IsFeatured = true
            }
        ];

        var cut = context.Render<News>();

        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Launch Story")));

        var carousel = cut.Find("section.news-carousel");
        carousel.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "ArrowRight" });

        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Studio Journal")));

        carousel.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "ArrowLeft" });

        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Launch Story")));
    }

    [Test]
    public void News_WhenEditorQueryIsNewAndUserIsAdmin_OpensCreateModal()
    {
        using var context = CreateContext(out _);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/news?editor=new");

        var cut = context.Render<News>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Create Article"));
            Assert.That(cut.Markup, Does.Contain("Database-backed news editor"));
            Assert.That(cut.Find("#news-title").GetAttribute("value"), Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void News_WhenCreateArticleTitleIsBlank_ShowsValidationErrorWithoutCallingCreate()
    {
        using var context = CreateContext(out var newsService);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/news?editor=new");

        var cut = context.Render<News>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Create Article"));
        });

        cut.Find("textarea#news-content").Change("Body copy");
        cut.Find("button.btn.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Title is required."));
        });

        Assert.That(newsService.CreateRequests, Is.Empty);
    }

    [Test]
    public void News_WhenCreateArticleContentIsBlank_ShowsValidationErrorWithoutCallingCreate()
    {
        using var context = CreateContext(out var newsService);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/news?editor=new");

        var cut = context.Render<News>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Create Article"));
        });

        cut.Find("#news-title").Change("New Story");
        cut.Find("button.btn.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Content is required."));
        });

        Assert.That(newsService.CreateRequests, Is.Empty);
    }

    [Test]
    public void News_WhenDeleteIsConfirmed_RemovesManagedImageAndDeletesArticle()
    {
        using var context = CreateContext(out var newsService, out var imageStorageService);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        newsService.Articles =
        [
            new NewsArticle
            {
                Id = "article-1",
                Title = "Launch Story",
                Slug = "launch-story",
                Content = "Full launch story content.",
                ImageUrl = "managed://launch-story",
                PublicationDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
                IsPublished = true,
                IsFeatured = false
            }
        ];
        newsService.ManagedImageUrls.Add("managed://launch-story");

        var cut = context.Render<News>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Launch Story"));
        });

        cut.Find("button.article-delete-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Delete Article"));
        });

        cut.Find("button.btn.btn-danger").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Not.Contain("Launch Story"));
            Assert.That(cut.Markup, Does.Contain("0 stored articles"));
        });

        Assert.That(newsService.DeletedIds, Is.EqualTo(["article-1"]));
        Assert.That(imageStorageService.DeletedStoragePaths, Is.EqualTo(["managed://launch-story"]));
    }

    private static BunitContext CreateContext(out FakeNewsArticleService newsService)
    {
        return CreateContext(out newsService, out _);
    }

    private static BunitContext CreateContext(out FakeNewsArticleService newsService, out FakeImageStorageService imageStorageService)
    {
        var context = new BunitContext();
        newsService = new FakeNewsArticleService();
        imageStorageService = new FakeImageStorageService();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton<INewsArticleService>(newsService);
        context.Services.AddSingleton<IImageStorageService>(imageStorageService);
        return context;
    }

    private sealed class FakeNewsArticleService : INewsArticleService
    {
        public List<NewsArticle> Articles { get; set; } = [];

        public List<NewsArticleUpdateRequest> CreateRequests { get; } = [];

        public List<NewsArticleUpdateRequest> UpdateRequests { get; } = [];

        public List<string> DeletedIds { get; } = [];

        public HashSet<string> ManagedImageUrls { get; } = [];

        public bool LastIncludeUnpublished { get; private set; }

        public Task<IReadOnlyList<NewsArticle>> GetArticlesAsync(bool includeUnpublished = false, CancellationToken cancellationToken = default)
        {
            LastIncludeUnpublished = includeUnpublished;
            IReadOnlyList<NewsArticle> result = Articles.ToList();
            return Task.FromResult(result);
        }

        public Task<NewsArticle> CreateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
        {
            CreateRequests.Add(CloneRequest(request));
            var created = new NewsArticle
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = request.Title,
                Slug = request.Title.Trim().ToLowerInvariant().Replace(' ', '-'),
                Content = request.Content,
                ImageUrl = request.ImageUrl ?? string.Empty,
                PublicationDateUtc = new DateTimeOffset(request.PublicationDate, TimeSpan.Zero),
                IsPublished = request.IsPublished,
                IsFeatured = request.IsFeatured
            };

            Articles.Add(created);
            return Task.FromResult(created);
        }

        public Task<NewsArticle> UpdateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
        {
            UpdateRequests.Add(CloneRequest(request));
            var existing = Articles.First(article => string.Equals(article.Slug, request.OriginalSlug, StringComparison.OrdinalIgnoreCase));
            existing.Title = request.Title;
            existing.Content = request.Content;
            existing.ImageUrl = request.ImageUrl ?? string.Empty;
            existing.PublicationDateUtc = new DateTimeOffset(request.PublicationDate, TimeSpan.Zero);
            existing.IsPublished = request.IsPublished;
            existing.IsFeatured = request.IsFeatured;
            return Task.FromResult(existing);
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            DeletedIds.Add(id);
            Articles.RemoveAll(article => string.Equals(article.Id, id, StringComparison.Ordinal));
            return Task.CompletedTask;
        }

        public bool IsManagedImageUrl(string? imageUrl) => !string.IsNullOrWhiteSpace(imageUrl) && ManagedImageUrls.Contains(imageUrl);

        private static NewsArticleUpdateRequest CloneRequest(NewsArticleUpdateRequest request)
        {
            return new NewsArticleUpdateRequest
            {
                OriginalSlug = request.OriginalSlug,
                Title = request.Title,
                Content = request.Content,
                ImageUrl = request.ImageUrl,
                PublicationDate = request.PublicationDate,
                IsPublished = request.IsPublished,
                IsFeatured = request.IsFeatured
            };
        }
    }

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public List<string> DeletedStoragePaths { get; } = [];

        public Task<ImageSaveResult> SaveReleaseImageAsync(IFormFile file, CancellationToken cancellationToken = default)
            => Task.FromResult(new ImageSaveResult
            {
                Url = "managed://uploaded-image"
            });

        public bool IsManagedImageUrl(string? imageUrl) => false;

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            DeletedStoragePaths.Add(storagePath);
            return Task.CompletedTask;
        }
    }
}
