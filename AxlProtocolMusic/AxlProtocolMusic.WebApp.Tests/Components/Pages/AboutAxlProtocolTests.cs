using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class AboutAxlProtocolTests
{
    [Test]
    public void AboutAxlProtocol_WhenContentExists_RendersPublicSections()
    {
        using var context = new BunitContext();
        var service = new FakeAboutPageService
        {
            Content = new AboutPageContent
            {
                HeroLead = "Synth artist and label architect.",
                HeroBody = "Building a living music universe.",
                FocusPoints = ["New releases", "Worldbuilding"],
                WhyThisSiteExistsMarkdown = "**Official home** for the catalog.",
                NarrativeHighlights = ["Release context", "Lyrics and credits"],
                OriginMarkdown = "Started as a midnight recording experiment.",
                Pillars =
                [
                    new AboutPillar { Title = "Story", Description = "Every release expands the world." }
                ]
            }
        };

        context.AddAuthorization().SetNotAuthorized();
        context.Services.AddSingleton<IAboutPageService>(service);
        context.Services.AddSingleton<MarkdownService>();

        var cut = context.Render<AboutAxlProtocol>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("About Axl Protocol"));
            Assert.That(cut.Markup, Does.Contain("Synth artist and label architect."));
            Assert.That(cut.Markup, Does.Contain("Building a living music universe."));
            Assert.That(cut.Markup, Does.Contain("New releases"));
            Assert.That(cut.Markup, Does.Contain("Worldbuilding"));
            Assert.That(cut.Markup, Does.Contain("Official home"));
            Assert.That(cut.Markup, Does.Contain("Release context"));
            Assert.That(cut.Markup, Does.Contain("Started as a midnight recording experiment."));
            Assert.That(cut.Markup, Does.Contain("Story"));
            Assert.That(cut.Markup, Does.Contain("Every release expands the world."));
            Assert.That(cut.Markup, Does.Contain("Browse Releases"));
            Assert.That(cut.Markup, Does.Contain("See Updates"));
            Assert.That(cut.Markup, Does.Contain("View Timeline"));
        });
    }

    [Test]
    public void AboutAxlProtocol_WhenListsAreEmpty_RendersEmptyStates()
    {
        using var context = new BunitContext();
        var service = new FakeAboutPageService
        {
            Content = new AboutPageContent
            {
                HeroLead = "Axl Protocol",
                HeroBody = "About page body."
            }
        };

        context.AddAuthorization().SetNotAuthorized();
        context.Services.AddSingleton<IAboutPageService>(service);
        context.Services.AddSingleton<MarkdownService>();

        var cut = context.Render<AboutAxlProtocol>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Focus points will appear here."));
            Assert.That(cut.Markup, Does.Contain("Narrative highlights will appear here."));
            Assert.That(cut.Markup, Does.Contain("Pillars Coming Soon"));
        });
    }

    private sealed class FakeAboutPageService : IAboutPageService
    {
        public AboutPageContent Content { get; set; } = new();

        public Task<AboutPageContent> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Content);

        public Task UpdateAsync(AboutPageContent content, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SeedAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
