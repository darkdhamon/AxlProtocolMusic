using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class ReleaseCreatePageTests
{
    [Test]
    public void ReleaseCreate_RendersSeededValuesAndKnownLists()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("admin");
        context.Services.AddSingleton<IReleaseService>(new FakeReleaseService());
        context.Services.AddSingleton<MarkdownService>();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/releases/new?Error=Bad%20input&Title=Signals&slug=signals&ShortDescription=Short%20copy&IsPublished=true");

        var cut = context.Render<ReleaseCreate>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Add Release"));
            Assert.That(cut.Markup, Does.Contain("Bad input"));
            Assert.That(cut.Find("form").GetAttribute("action"), Is.EqualTo("/releases/create"));
            Assert.That(cut.Find("#title").GetAttribute("value"), Is.EqualTo("Signals"));
            Assert.That(cut.Find("#slug").GetAttribute("value"), Is.EqualTo("signals"));
            Assert.That(cut.Markup, Does.Contain("Production"));
            Assert.That(cut.Markup, Does.Contain("Axl Protocol"));
            Assert.That(cut.Markup, Does.Contain("Synthwave"));
            Assert.That(cut.Markup, Does.Contain("Computed type:"));
            Assert.That(cut.Markup, Does.Contain(">Single</strong>"));
        });
    }

    [Test]
    public void ReleaseCreate_WhenTitleChanges_AutoGeneratesSlugUntilManuallyEdited()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("admin");
        var releaseService = new FakeReleaseService();
        context.Services.AddSingleton<IReleaseService>(releaseService);
        context.Services.AddSingleton<MarkdownService>();

        var cut = context.Render<ReleaseCreate>();

        cut.Find("#title").Input("Neon Bloom");

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("#slug").GetAttribute("value"), Is.EqualTo("generated-neon-bloom"));
        });

        cut.Find("#slug").Input("custom-slug");
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("#slug").GetAttribute("value"), Is.EqualTo("generated-custom-slug"));
        });

        cut.Find("#title").Input("Will Not Override");
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("#slug").GetAttribute("value"), Is.EqualTo("generated-custom-slug"));
        });
    }

    private sealed class FakeReleaseService : IReleaseService
    {
        public Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FeaturedReleaseViewModel>>([]);

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
            => Task.FromResult($"generated-{(value ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '-')}");

        public Task<IReadOnlyList<string>> GetKnownCreditRolesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Production"]);

        public Task<IReadOnlyList<string>> GetKnownContributorNamesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Axl Protocol"]);

        public Task<IReadOnlyList<string>> GetKnownTagsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Synthwave"]);

        public bool IsManagedImageUrl(string? imageUrl) => false;
    }
}
