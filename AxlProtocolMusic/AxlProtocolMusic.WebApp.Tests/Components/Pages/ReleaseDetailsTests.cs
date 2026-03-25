using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class ReleaseDetailsTests
{
    [Test]
    public void ReleaseDetails_WhenReleaseIsMissing_RendersNotFoundState()
    {
        using var context = CreateContext(out var releaseService);
        releaseService.Release = null;

        var cut = context.Render<ReleaseDetails>(parameters => parameters
            .Add(component => component.Slug, "missing-release"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Release Not Found"));
            Assert.That(cut.Markup, Does.Contain("The release you requested could not be found."));
            Assert.That(cut.Markup, Does.Contain("href=\"/releases\""));
        });

        Assert.That(releaseService.RequestedSlug, Is.EqualTo("missing-release"));
        Assert.That(releaseService.LastIncludeUnpublished, Is.False);
    }

    [Test]
    public void ReleaseDetails_WhenReleaseExists_RendersPublicDetails()
    {
        using var context = CreateContext(out var releaseService);
        releaseService.Release = new ReleaseDetailsViewModel
        {
            Id = "release-1",
            Title = "Signals",
            Slug = "signals",
            ShortDescription = "A cinematic synth release.",
            CoverImageUrl = "https://cdn.example/signals.jpg",
            Story = "**Made at midnight.**",
            ReleaseType = "EP",
            Tags = ["Synthwave", "Instrumental"],
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(30),
            Credits =
            [
                new ReleaseCredit { Name = "Axl Protocol", Roles = ["Production", "Mix"] }
            ],
            Tracks =
            [
                new ReleaseTrack { Title = "Neon Run", Duration = "3:45", Lyrics = "Night drive" },
                new ReleaseTrack { Title = "Static Bloom", Duration = "4:15" }
            ],
            Links =
            [
                new ReleaseLink { PlatformName = "Bandcamp", Url = "https://bandcamp.example/signals" }
            ]
        };

        var cut = context.Render<ReleaseDetails>(parameters => parameters
            .Add(component => component.Slug, "signals"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("h1").TextContent, Is.EqualTo("Signals"));
            Assert.That(cut.Markup, Does.Contain("A cinematic synth release."));
            Assert.That(cut.Markup, Does.Contain("Upcoming Release"));
            Assert.That(cut.Markup, Does.Contain("Synthwave"));
            Assert.That(cut.Markup, Does.Contain("Instrumental"));
            Assert.That(cut.Markup, Does.Contain("Bandcamp"));
            Assert.That(cut.Markup, Does.Contain("Made at midnight."));
            Assert.That(cut.Markup, Does.Contain("Total Play Time:</strong> 8:00"));
            Assert.That(cut.Markup, Does.Contain("Axl Protocol"));
            Assert.That(cut.Markup, Does.Contain("Production, Mix"));
            Assert.That(cut.Markup, Does.Contain("Neon Run"));
            Assert.That(cut.Markup, Does.Contain("Static Bloom"));
        });

        var image = cut.Find("img");
        Assert.That(image.GetAttribute("src"), Is.EqualTo("https://cdn.example/signals.jpg"));
        Assert.That(releaseService.LastIncludeUnpublished, Is.False);
    }

    [Test]
    public void ReleaseDetails_WhenAdminIsViewing_RequestsUnpublishedRelease()
    {
        using var context = CreateContext(out var releaseService);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        releaseService.Release = new ReleaseDetailsViewModel
        {
            Id = "release-2",
            Title = "Vault",
            Slug = "vault",
            ShortDescription = "Private preview.",
            ReleaseType = "Single",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var cut = context.Render<ReleaseDetails>(parameters => parameters
            .Add(component => component.Slug, "vault"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Vault"));
        });

        Assert.That(releaseService.LastIncludeUnpublished, Is.True);
    }

    private static BunitContext CreateContext(out FakeReleaseService releaseService)
    {
        var context = new BunitContext();
        releaseService = new FakeReleaseService();
        context.AddAuthorization().SetNotAuthorized();
        context.Services.AddSingleton<IReleaseService>(releaseService);
        context.Services.AddSingleton<IImageStorageService, FakeImageStorageService>();
        context.Services.AddSingleton<MarkdownService>();
        return context;
    }

    private sealed class FakeReleaseService : IReleaseService
    {
        public ReleaseDetailsViewModel? Release { get; set; }

        public string? RequestedSlug { get; private set; }

        public bool LastIncludeUnpublished { get; private set; }

        public Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FeaturedReleaseViewModel>>([]);

        public Task<PagedReleaseResult> GetPagedReleasesAsync(string? searchTerm, int pageNumber, int pageSize, bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedReleaseResult());

        public Task<ReleaseDetailsViewModel?> GetReleaseBySlugAsync(string slug, bool includeUnpublished = false, CancellationToken cancellationToken = default)
        {
            RequestedSlug = slug;
            LastIncludeUnpublished = includeUnpublished;
            return Task.FromResult(Release);
        }

        public Task<ReleaseUpdateResult> UpdateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseUpdateResult());

        public Task<ReleaseCreateResult> CreateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseCreateResult());

        public Task<ReleaseDeleteResult> DeleteReleaseAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseDeleteResult());

        public Task<string> GenerateUniqueSlugAsync(string? value, CancellationToken cancellationToken = default)
            => Task.FromResult(value ?? string.Empty);

        public Task<IReadOnlyList<string>> GetKnownCreditRolesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Production", "Mix"]);

        public Task<IReadOnlyList<string>> GetKnownContributorNamesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Axl Protocol"]);

        public Task<IReadOnlyList<string>> GetKnownTagsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Synthwave", "Instrumental"]);

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
