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

    [Test]
    public void ReleaseCreate_WhenCreditsRolesTracksLinksAndTagsAreManaged_UpdatesFormState()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("admin");
        context.Services.AddSingleton<IReleaseService>(new FakeReleaseService());
        context.Services.AddSingleton<MarkdownService>();

        var cut = context.Render<ReleaseCreate>();

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Add Credit").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.FindAll("input[id^='credit-name-']"), Has.Count.EqualTo(1));
        });

        cut.Find("#credit-name-0").Input("Axl Protocol");
        cut.Find("#credit-role-input-0").Input("Production");
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Add Role").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Production x"));
            Assert.That(cut.FindAll("input[type='hidden'][name='Credits[0].Roles[0]']"), Has.Count.EqualTo(1));
        });

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Add Track").Click();
        cut.Find("#track-title-0").Input("Neon Run");
        cut.Find("#track-duration-0").Input("3:45");
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain(">Single</strong>"));
        });

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Add Link").Click();
        cut.Find("#link-platform-0").Input("Bandcamp");
        cut.Find("#link-url-0").Input("https://bandcamp.example/neon-run");
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("#link-platform-0").GetAttribute("value"), Is.EqualTo("Bandcamp"));
            Assert.That(cut.Find("#link-url-0").GetAttribute("value"), Is.EqualTo("https://bandcamp.example/neon-run"));
        });

        cut.Find("#tag-input").Input("Synthwave");
        cut.FindAll("button").Where(button => button.TextContent.Trim() == "Add Tag").Last().Click();
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Synthwave x"));
            Assert.That(cut.FindAll("input[type='hidden'][name='Tags[0]']"), Has.Count.EqualTo(1));
        });

        cut.FindAll("button.role-chip-edit").Single(button => button.TextContent.Trim() == "Production x").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Not.Contain("Production x"));
        });

        cut.FindAll("button.role-chip-edit").Single(button => button.TextContent.Trim() == "Synthwave x").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Not.Contain("Synthwave x"));
        });
    }

    [Test]
    public void ReleaseCreate_WhenReleaseTypeOverrideChanges_ShowsOverrideValue()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("admin");
        context.Services.AddSingleton<IReleaseService>(new FakeReleaseService());
        context.Services.AddSingleton<MarkdownService>();

        var cut = context.Render<ReleaseCreate>();

        cut.Find("#releaseTypeOverride").Change("Album");

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain(">Album</strong>"));
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
