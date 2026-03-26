using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class HomePageTests
{
    [Test]
    public void Home_WhenNoFeaturedReleasesExist_RendersEmptyState()
    {
        using var context = new BunitContext();
        var releaseService = new FakeHomeReleaseService();
        context.Services.AddSingleton<IReleaseService>(releaseService);

        var cut = context.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Featured releases will appear here as soon as published music is added to the catalog."));
            Assert.That(cut.Markup, Does.Contain("Browse Releases"));
            Assert.That(cut.Markup, Does.Contain("About Axl Protocol"));
            Assert.That(cut.Markup, Does.Contain("News Articles"));
            Assert.That(cut.Markup, Does.Contain("Timeline"));
        });
    }

    [Test]
    public void Home_WhenFeaturedReleasesExist_RendersActiveRelease()
    {
        using var context = new BunitContext();
        var releaseService = new FakeHomeReleaseService
        {
            FeaturedReleases =
            [
                new FeaturedReleaseViewModel
                {
                    Title = "Signals",
                    Slug = "signals",
                    ShortDescription = "A cinematic synth release.",
                    CoverImageUrl = "https://cdn.example/signals.jpg",
                    ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(5)
                },
                new FeaturedReleaseViewModel
                {
                    Title = "Echo Grid",
                    Slug = "echo-grid",
                    ShortDescription = "Second featured release.",
                    CoverImageUrl = string.Empty,
                    ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-30)
                }
            ]
        };
        context.Services.AddSingleton<IReleaseService>(releaseService);

        var cut = context.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Signals"));
            Assert.That(cut.Markup, Does.Contain("A cinematic synth release."));
            Assert.That(cut.Markup, Does.Contain("View Release"));
            Assert.That(cut.Markup, Does.Contain("/releases/signals"));
            Assert.That(cut.Markup, Does.Contain("Show release 1"));
            Assert.That(cut.Markup, Does.Contain("Show release 2"));
            Assert.That(cut.Markup, Does.Contain("Coming Soon"));
            Assert.That(cut.Markup, Does.Contain(releaseService.FeaturedReleases[0].ReleaseDateUtc.ToLocalTime().ToString("MMMM dd, yyyy")));
            Assert.That(cut.Markup, Does.Contain("class=\"is-upcoming\""));
        });

        var image = cut.Find("img");
        Assert.That(image.GetAttribute("src"), Is.EqualTo("https://cdn.example/signals.jpg"));
    }

    [Test]
    public void Home_WhenArrowKeysArePressed_CyclesFeaturedReleases()
    {
        using var context = new BunitContext();
        var releaseService = new FakeHomeReleaseService
        {
            FeaturedReleases =
            [
                new FeaturedReleaseViewModel
                {
                    Title = "Signals",
                    Slug = "signals",
                    ShortDescription = "A cinematic synth release.",
                    ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(5)
                },
                new FeaturedReleaseViewModel
                {
                    Title = "Echo Grid",
                    Slug = "echo-grid",
                    ShortDescription = "Second featured release.",
                    ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-30)
                }
            ]
        };
        context.Services.AddSingleton<IReleaseService>(releaseService);

        var cut = context.Render<Home>();

        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Signals")));

        var carousel = cut.Find("section.hero-carousel");
        carousel.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "ArrowRight" });

        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Echo Grid")));

        carousel.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "ArrowLeft" });

        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Signals")));
    }

    private sealed class FakeHomeReleaseService : IReleaseService
    {
        public IReadOnlyList<FeaturedReleaseViewModel> FeaturedReleases { get; set; } = [];

        public Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(FeaturedReleases);

        public Task<PagedReleaseResult> GetPagedReleasesAsync(string? searchTerm, int pageNumber, int pageSize, bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedReleaseResult());

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

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetKnownContributorRolesByNameAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(new Dictionary<string, IReadOnlyList<string>>());

        public Task<IReadOnlyList<string>> GetKnownTagsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public bool IsManagedImageUrl(string? imageUrl) => false;
    }
}
