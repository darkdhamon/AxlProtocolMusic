using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class NotFoundPageTests
{
    [Test]
    public void NotFound_RendersCustom404Content()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<INewsArticleService>(new FakeNewsArticleService
        {
            Articles =
            [
                new NewsArticle
                {
                    Id = "news-1",
                    Title = "Signal Boost",
                    Slug = "signal-boost",
                    Content = "A new studio update with fresh details from the latest session.",
                    ImageUrl = "https://cdn.example/news.jpg",
                    PublicationDateUtc = DateTimeOffset.UtcNow.AddDays(-2),
                    IsPublished = true
                }
            ]
        });
        context.Services.AddSingleton<IReleaseService>(new FakeReleaseService
        {
            Result = new PagedReleaseResult
            {
                Items =
                [
                    new ReleaseListItemViewModel
                    {
                        Title = "Neon Static",
                        Slug = "neon-static",
                        ShortDescription = "A late-night synth release built for the city lights.",
                        CoverImageUrl = "https://cdn.example/release.jpg",
                        IsPublished = true
                    }
                ]
            }
        });

        var cut = context.Render<NotFound>();

        Assert.That(cut.Markup, Does.Contain("404 Error"));
        Assert.That(cut.Markup, Does.Contain("Page not found"));
        Assert.That(cut.Markup, Does.Contain("The page you requested does not exist or may have moved."));
        Assert.That(cut.Markup, Does.Contain("Use the site navigation to get back to the section you were looking for."));
        Assert.That(cut.Markup, Does.Contain("You Might Like"));
        Assert.That(cut.Markup, Does.Contain("Signal Boost"));
        Assert.That(cut.Markup, Does.Contain("Neon Static"));
        Assert.That(cut.Markup, Does.Contain("View Article"));
        Assert.That(cut.Markup, Does.Contain("View Release"));
        Assert.That(cut.Markup, Does.Contain("/news?article=signal-boost"));
        Assert.That(cut.Markup, Does.Contain("/releases/neon-static"));

        var image = cut.Find("img");
        Assert.That(image.GetAttribute("src"), Is.EqualTo("/Assets/Misc/404-Graphic.png"));
        Assert.That(image.GetAttribute("alt"), Is.EqualTo("Stylized 404 graphic"));
    }

    private sealed class FakeNewsArticleService : INewsArticleService
    {
        public IReadOnlyList<NewsArticle> Articles { get; set; } = [];

        public Task<IReadOnlyList<NewsArticle>> GetArticlesAsync(bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Articles);

        public Task<NewsArticle> CreateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new NewsArticle());

        public Task<NewsArticle> UpdateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new NewsArticle());

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public bool IsManagedImageUrl(string? imageUrl) => false;
    }

    private sealed class FakeReleaseService : IReleaseService
    {
        public PagedReleaseResult Result { get; set; } = new();

        public Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FeaturedReleaseViewModel>>([]);

        public Task<PagedReleaseResult> GetPagedReleasesAsync(string? searchTerm, int pageNumber, int pageSize, bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Result);

        public Task<ReleaseDetailsViewModel?> GetReleaseBySlugAsync(string slug, bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => Task.FromResult<ReleaseDetailsViewModel?>(null);

        public Task<ReleaseUpdateResult> UpdateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseUpdateResult());

        public Task<ReleaseCreateResult> CreateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseCreateResult());

        public Task<ReleaseDeleteResult> DeleteReleaseAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseDeleteResult());

        public Task<string> GenerateUniqueSlugAsync(string? value, CancellationToken cancellationToken = default)
            => Task.FromResult(value ?? string.Empty);

        public Task<IReadOnlyList<string>> GetKnownCreditRolesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<string>> GetKnownContributorNamesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<string>> GetKnownTagsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public bool IsManagedImageUrl(string? imageUrl) => false;
    }
}
