using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
                ],
                SocialLinks =
                [
                    new AboutSocialLink { Platform = "Instagram", Url = "https://instagram.com/axlprotocolmusic" },
                    new AboutSocialLink { Platform = "Spotify", Url = "https://open.spotify.com/artist/example" }
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
            Assert.That(cut.Markup, Does.Contain("Social And Streaming Links"));
            Assert.That(cut.Markup, Does.Contain("Instagram"));
            Assert.That(cut.Markup, Does.Contain("open.spotify.com/artist/example"));
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
            Assert.That(cut.Markup, Does.Contain("Links Coming Soon"));
        });
    }

    [Test]
    public void AboutAxlProtocol_WhenSocialLinksContainInvalidEntries_RendersOnlySafeLinks()
    {
        using var context = new BunitContext();
        var service = new FakeAboutPageService
        {
            Content = new AboutPageContent
            {
                HeroLead = "Axl Protocol",
                HeroBody = "About page body.",
                SocialLinks =
                [
                    new AboutSocialLink { Platform = "YouTube", Url = "https://www.youtube.com/@AxlProtocol" },
                    new AboutSocialLink { Platform = "Unsafe", Url = "javascript:alert(1)" },
                    new AboutSocialLink { Platform = "Incomplete", Url = "" }
                ]
            }
        };

        context.AddAuthorization().SetNotAuthorized();
        context.Services.AddSingleton<IAboutPageService>(service);
        context.Services.AddSingleton<MarkdownService>();

        var cut = context.Render<AboutAxlProtocol>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("YouTube"));
            Assert.That(cut.Markup, Does.Not.Contain("Unsafe"));
            Assert.That(cut.Markup, Does.Not.Contain("Incomplete"));
            Assert.That(cut.Markup, Does.Not.Contain("javascript:alert(1)"));
            Assert.That(cut.Markup, Does.Not.Contain("Links Coming Soon"));
        });
    }

    [Test]
    public void AboutAxlProtocol_WhenLegacyContentHasNullSocialLinks_RendersEmptyLinkState()
    {
        using var context = new BunitContext();
        var content = new AboutPageContent
        {
            HeroLead = "Axl Protocol",
            HeroBody = "About page body."
        };
        content.SocialLinks = null!;

        var service = new FakeAboutPageService
        {
            Content = content
        };

        context.AddAuthorization().SetNotAuthorized();
        context.Services.AddSingleton<IAboutPageService>(service);
        context.Services.AddSingleton<MarkdownService>();

        var cut = context.Render<AboutAxlProtocol>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Social And Streaming Links"));
            Assert.That(cut.Markup, Does.Contain("Links Coming Soon"));
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

        var errorAlert = cut.Find("div.alert.alert-danger[role='alert']");
        Assert.That(errorAlert.TextContent, Does.Contain("Save failed."));
        Assert.That(errorAlert.HasAttribute("aria-live"), Is.False);
        Assert.That(errorAlert.GetAttribute("aria-atomic"), Is.EqualTo("true"));
    }

    [Test]
    public void AboutAxlProtocol_WhenAutosaveStarts_TransitionsThroughSavingAndSavedMessages()
    {
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");

        var updateCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeAboutPageService
        {
            Content = new AboutPageContent
            {
                HeroLead = "Lead",
                HeroBody = "Body",
                FocusPoints = ["Existing focus point"]
            }
        };
        service.UpdateCompletions.Enqueue(updateCompletion);

        context.Services.AddSingleton<IAboutPageService>(service);
        context.Services.AddSingleton<MarkdownService>();
        context.Services.AddSingleton<IOptions<EditorSettings>>(Options.Create(new EditorSettings
        {
            AutosaveDelayMilliseconds = 25
        }));

        var cut = context.Render<AboutAxlProtocol>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Edit About Page"));
            Assert.That(cut.Markup, Does.Not.Contain("Saving changes..."));
            Assert.That(cut.Find("div[role='status']").TextContent, Is.Empty);
        });

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Add Point", StringComparison.Ordinal))
            .Click();

        Assert.That(SpinWait.SpinUntil(() => service.UpdateCallCount >= 1, TimeSpan.FromSeconds(3)), Is.True);

        cut.WaitForAssertion(() =>
        {
            var statusRegion = cut.Find("div[role='status']");
            Assert.That(cut.Markup, Does.Contain("Saving changes..."));
            Assert.That(statusRegion.GetAttribute("aria-live"), Is.EqualTo("polite"));
            Assert.That(statusRegion.GetAttribute("aria-atomic"), Is.EqualTo("true"));
        }, timeout: TimeSpan.FromSeconds(3));

        updateCompletion.SetResult();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("All changes saved."));
        }, timeout: TimeSpan.FromSeconds(4));

        Assert.That(cut.Markup, Does.Contain("All changes saved."));
        Assert.That(cut.Markup, Does.Not.Contain("Saving changes..."));
    }

    [Test]
    public void AboutAxlProtocol_WhenNewerAutosaveIsPending_DoesNotPublishStaleSavedStatus()
    {
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");

        var firstUpdateCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondUpdateCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeAboutPageService
        {
            Content = new AboutPageContent
            {
                HeroLead = "Lead",
                HeroBody = "Body",
                FocusPoints = ["Existing focus point"]
            }
        };
        service.UpdateCompletions.Enqueue(firstUpdateCompletion);
        service.UpdateCompletions.Enqueue(secondUpdateCompletion);

        context.Services.AddSingleton<IAboutPageService>(service);
        context.Services.AddSingleton<MarkdownService>();
        context.Services.AddSingleton<IOptions<EditorSettings>>(Options.Create(new EditorSettings
        {
            AutosaveDelayMilliseconds = 25
        }));

        var cut = context.Render<AboutAxlProtocol>();
        var addPointButton = cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Add Point", StringComparison.Ordinal));

        addPointButton.Click();
        Assert.That(SpinWait.SpinUntil(() => service.UpdateCallCount >= 1, TimeSpan.FromSeconds(3)), Is.True);

        addPointButton.Click();
        firstUpdateCompletion.SetResult();

        Assert.That(SpinWait.SpinUntil(() => service.UpdateCallCount >= 2, TimeSpan.FromSeconds(3)), Is.True);
        Assert.That(cut.Markup, Does.Contain("Saving changes..."));
        Assert.That(cut.Markup, Does.Not.Contain("All changes saved."));

        secondUpdateCompletion.SetResult();
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("All changes saved."));
        }, timeout: TimeSpan.FromSeconds(3));
    }

    private sealed class FakeAboutPageService : IAboutPageService
    {
        public AboutPageContent Content { get; set; } = new();

        public int UpdateCallCount { get; private set; }

        public AboutPageContent? LastUpdatedContent { get; private set; }

        public Exception? UpdateException { get; set; }

        public Queue<TaskCompletionSource> UpdateCompletions { get; } = new();

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

            return UpdateCompletions.Count > 0
                ? UpdateCompletions.Dequeue().Task
                : Task.CompletedTask;
        }

        public Task SeedAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
