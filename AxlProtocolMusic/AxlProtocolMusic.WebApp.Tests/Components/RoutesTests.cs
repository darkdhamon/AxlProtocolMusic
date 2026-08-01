using AxlProtocolMusic.WebApp.Components;
using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components;

[TestFixture]
public sealed class RoutesTests
{
    [Test]
    public void Routes_WhenUserIsAnonymous_RoutesToLoginOnProtectedPage()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetNotAuthorized();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        context.Services.AddSingleton<INewsArticleService>(new FakeNewsArticleService());
        context.Services.AddSingleton<IReleaseService>(new FakeReleaseService());
        navigation.NavigateTo("https://localhost/admin");

        var cut = context.Render<Routes>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(navigation.Uri, Is.EqualTo("https://localhost/login?returnUrl=%2Fadmin"));
        });
    }

    [Test]
    public void Routes_WhenUserLacksAdminRole_GoesToAccessDenied()
    {
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("viewer");
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        context.Services.AddSingleton<INewsArticleService>(new FakeNewsArticleService());
        context.Services.AddSingleton<IReleaseService>(new FakeReleaseService());
        navigation.NavigateTo("https://localhost/admin");

        var cut = context.Render<Routes>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(navigation.Uri, Does.EndWith("/access-denied"));
        });
    }

    [Test]
    public void Routes_WhenRouteIsMissing_ShowsNotFoundPage()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<INewsArticleService>(new FakeNewsArticleService
        {
            Articles =
            [
                new NewsArticle
                {
                    Id = "news-1",
                    Title = "Test article",
                    Slug = "test-article",
                    PublicationDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
                    IsPublished = true
                }
            ]
        });
        context.Services.AddSingleton<IReleaseService>(new FakeReleaseService
        {
            SearchResults =
            [
                new ReleaseListItemViewModel
                {
                    Title = "Test release",
                    Slug = "test-release",
                    ShortDescription = "A test release",
                    IsPublished = true
                }
            ]
        });
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("https://localhost/this-route-does-not-exist");

        var cut = context.Render<Routes>();

        Assert.That(cut.Markup, Does.Contain("404 Error"));
        Assert.That(cut.Markup, Does.Contain("Page not found"));
        Assert.That(cut.Markup, Does.Contain("The page you requested does not exist or may have moved."));
    }

    private sealed class FakeNewsArticleService : INewsArticleService
    {
        public IReadOnlyList<NewsArticle> Articles { get; set; } = [];

        public Task<IReadOnlyList<NewsArticle>> GetArticlesAsync(
            bool includeUnpublished = false,
            CancellationToken cancellationToken = default)
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
        public IReadOnlyList<FeaturedReleaseViewModel> FeaturedReleases { get; set; } = [];

        public IReadOnlyList<ReleaseListItemViewModel> SearchResults { get; set; } = [];

        public Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(FeaturedReleases);

        public Task<PagedReleaseResult> GetPagedReleasesAsync(
            string? searchTerm,
            int pageNumber,
            int pageSize,
            bool includeUnpublished = false,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedReleaseResult
            {
                Items = SearchResults
            });

        public Task<ReleaseDetailsViewModel?> GetReleaseBySlugAsync(
            string slug,
            bool includeUnpublished = false,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ReleaseDetailsViewModel?>(null);

        public Task<ReleaseUpdateResult> UpdateReleaseAsync(
            ReleaseUpdateRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseUpdateResult());

        public Task<ReleaseCreateResult> CreateReleaseAsync(
            ReleaseUpdateRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseCreateResult());

        public Task<ReleaseDeleteResult> DeleteReleaseAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseDeleteResult());

        public Task<string> GenerateUniqueSlugAsync(string? value, CancellationToken cancellationToken = default)
            => Task.FromResult(value ?? string.Empty);

        public Task<IReadOnlyList<string>> GetKnownCreditRolesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<string>> GetKnownContributorNamesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetKnownContributorRolesByNameAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(new Dictionary<string, IReadOnlyList<string>>());

        public Task<IReadOnlyList<string>> GetKnownTagsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public bool IsManagedImageUrl(string? imageUrl) => false;
    }
}
