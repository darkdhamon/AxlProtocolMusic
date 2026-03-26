using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class ReleasesPageTests
{
    [Test]
    public void Releases_WhenNoResultsExist_RendersEmptyState()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetNotAuthorized();
        context.Services.AddSingleton<IReleaseService>(new FakePagedReleaseService
        {
            PagedResult = new PagedReleaseResult
            {
                Items = [],
                PageNumber = 1,
                PageSize = 6,
                TotalCount = 0,
                TotalPages = 0,
                SearchTerm = string.Empty
            }
        });

        var cut = context.Render<Releases>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("No releases found"));
            Assert.That(cut.Markup, Does.Contain("Try a different search or clear the filter to see the full catalog."));
        });
    }

    [Test]
    public void Releases_WhenResultsExist_RendersReleaseCards()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetNotAuthorized();
        var releaseService = new FakePagedReleaseService
        {
            PagedResult = new PagedReleaseResult
            {
                Items =
                [
                    new ReleaseListItemViewModel
                    {
                        Title = "Signals",
                        Slug = "signals",
                        ShortDescription = "A cinematic synth release.",
                        CoverImageUrl = "https://cdn.example/signals.jpg",
                        ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(15),
                        IsPublished = true
                    },
                    new ReleaseListItemViewModel
                    {
                        Title = "Echo Grid",
                        Slug = "echo-grid",
                        ShortDescription = "Second catalog entry.",
                        CoverImageUrl = string.Empty,
                        ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-20),
                        IsPublished = true
                    }
                ],
                PageNumber = 1,
                PageSize = 6,
                TotalCount = 2,
                TotalPages = 1,
                SearchTerm = string.Empty
            }
        };
        context.Services.AddSingleton<IReleaseService>(releaseService);

        var cut = context.Render<Releases>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Showing 2 of 2 releases"));
            Assert.That(cut.Markup, Does.Contain("Signals"));
            Assert.That(cut.Markup, Does.Contain("Echo Grid"));
            Assert.That(cut.Markup, Does.Contain("Upcoming Release"));
            Assert.That(cut.Markup, Does.Contain("Coming Soon"));
            Assert.That(cut.Markup, Does.Contain("release-coming-soon-group"));
            Assert.That(cut.Markup, Does.Contain(releaseService.PagedResult.Items[0].ReleaseDateUtc.ToLocalTime().ToString("MMMM dd, yyyy")));
            Assert.That(cut.Markup, Does.Contain("release-artwork is-upcoming"));
            Assert.That(cut.Markup, Does.Contain("/releases/signals"));
            Assert.That(cut.Markup, Does.Contain("/releases/echo-grid"));
            Assert.That(cut.Markup, Does.Contain("All visible releases are loaded."));
        });
    }

    private sealed class FakePagedReleaseService : IReleaseService
    {
        public PagedReleaseResult PagedResult { get; set; } = new();

        public Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FeaturedReleaseViewModel>>([]);

        public Task<PagedReleaseResult> GetPagedReleasesAsync(string? searchTerm, int pageNumber, int pageSize, bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => Task.FromResult(PagedResult);

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
