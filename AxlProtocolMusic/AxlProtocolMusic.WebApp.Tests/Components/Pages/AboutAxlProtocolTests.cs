using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Bunit;
using Bunit.TestDoubles;
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

    [Test]
    public void AboutAxlProtocol_WhenAdminAddsFocusPoint_AutosavesAndShowsSuccess()
    {
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");

        var service = new FakeAboutPageService
        {
            Content = new AboutPageContent
            {
                HeroLead = "Lead",
                HeroBody = "Body"
            }
        };

        context.Services.AddSingleton<IAboutPageService>(service);
        context.Services.AddSingleton<MarkdownService>();

        var cut = context.Render<AboutAxlProtocol>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Edit About Page"));
        });

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Add Point", StringComparison.Ordinal))
            .Click();

        Assert.That(SpinWait.SpinUntil(() => service.UpdateCallCount >= 1, TimeSpan.FromSeconds(3)), Is.True);
        cut.Render();

        Assert.That(service.LastUpdatedContent, Is.Not.Null);
        Assert.That(service.LastUpdatedContent!.FocusPoints, Has.Count.EqualTo(1));
        Assert.That(cut.Markup, Does.Contain("All changes saved."));
    }

    [Test]
    public void AboutAxlProtocol_WhenAutosaveFails_ShowsErrorMessage()
    {
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");

        var service = new FakeAboutPageService
        {
            Content = new AboutPageContent
            {
                HeroLead = "Lead",
                HeroBody = "Body",
                FocusPoints = ["Existing focus point"]
            },
            UpdateException = new InvalidOperationException("Save failed.")
        };

        context.Services.AddSingleton<IAboutPageService>(service);
        context.Services.AddSingleton<MarkdownService>();

        var cut = context.Render<AboutAxlProtocol>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Edit About Page"));
        });

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Remove", StringComparison.Ordinal))
            .Click();

        Assert.That(SpinWait.SpinUntil(() => service.UpdateCallCount >= 1, TimeSpan.FromSeconds(3)), Is.True);

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Save failed."));
        }, timeout: TimeSpan.FromSeconds(3));
    }

    private sealed class FakeAboutPageService : IAboutPageService
    {
        public AboutPageContent Content { get; set; } = new();

        public int UpdateCallCount { get; private set; }

        public AboutPageContent? LastUpdatedContent { get; private set; }

        public Exception? UpdateException { get; set; }

        public Task<AboutPageContent> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Content);

        public Task UpdateAsync(AboutPageContent content, CancellationToken cancellationToken = default)
        {
            UpdateCallCount++;
            LastUpdatedContent = new AboutPageContent
            {
                Id = content.Id,
                HeroLead = content.HeroLead,
                HeroBody = content.HeroBody,
                FocusPoints = content.FocusPoints.ToList(),
                WhyThisSiteExistsMarkdown = content.WhyThisSiteExistsMarkdown,
                NarrativeHighlights = content.NarrativeHighlights.ToList(),
                OriginMarkdown = content.OriginMarkdown,
                Pillars = content.Pillars
                    .Select(pillar => new AboutPillar
                    {
                        Title = pillar.Title,
                        Description = pillar.Description
                    })
                    .ToList()
            };

            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            return Task.CompletedTask;
        }

        public Task SeedAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
