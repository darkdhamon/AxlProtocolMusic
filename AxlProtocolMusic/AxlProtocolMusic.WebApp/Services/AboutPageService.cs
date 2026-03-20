using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Interfaces;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class AboutPageService : IAboutPageService
{
    private readonly IRepository<AboutPageContent> _aboutRepository;

    public AboutPageService(IRepository<AboutPageContent> aboutRepository)
    {
        _aboutRepository = aboutRepository;
    }

    public async Task<AboutPageContent> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _aboutRepository.GetByIdAsync(AboutPageContent.SingletonId, cancellationToken)
            ?? CreateDefaultContent();
    }

    public async Task UpdateAsync(AboutPageContent content, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(content);
        var existing = await _aboutRepository.GetByIdAsync(AboutPageContent.SingletonId, cancellationToken);

        if (existing is null)
        {
            await _aboutRepository.CreateAsync(normalized, cancellationToken);
            return;
        }

        await _aboutRepository.UpdateAsync(normalized, cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _aboutRepository.GetByIdAsync(AboutPageContent.SingletonId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        await _aboutRepository.CreateAsync(CreateDefaultContent(), cancellationToken);
    }

    private static AboutPageContent Normalize(AboutPageContent content)
    {
        return new AboutPageContent
        {
            Id = AboutPageContent.SingletonId,
            HeroLead = content.HeroLead.Trim(),
            HeroBody = content.HeroBody.Trim(),
            FocusPoints = content.FocusPoints
                .Select(point => point.Trim())
                .Where(point => !string.IsNullOrWhiteSpace(point))
                .ToList(),
            WhyThisSiteExistsMarkdown = content.WhyThisSiteExistsMarkdown.Trim(),
            NarrativeHighlights = content.NarrativeHighlights
                .Select(point => point.Trim())
                .Where(point => !string.IsNullOrWhiteSpace(point))
                .ToList(),
            OriginMarkdown = content.OriginMarkdown.Trim(),
            Pillars = content.Pillars
                .Select(pillar => new AboutPillar
                {
                    Title = pillar.Title.Trim(),
                    Description = pillar.Description.Trim()
                })
                .Where(pillar => !string.IsNullOrWhiteSpace(pillar.Title) || !string.IsNullOrWhiteSpace(pillar.Description))
                .ToList()
        };
    }

    private static AboutPageContent CreateDefaultContent()
    {
        return new AboutPageContent
        {
            Id = AboutPageContent.SingletonId,
            HeroLead = "Axl Protocol is both the voice and the world behind Axl Protocol Music, a project built to release songs with more story, context, and emotional history than streaming platforms can usually hold.",
            HeroBody = "This site is where the catalog becomes more than a list of titles. It is where releases gain memory, collaborators get named, and the story behind the music can live in full.",
            FocusPoints =
            [
                "Releases presented with story, credits, lyrics, and listening links",
                "A growing artist world shaped by narrative, identity, and lived context",
                "A label home base that can evolve as the project expands beyond one name"
            ],
            WhyThisSiteExistsMarkdown = """
## More Than A Streaming Profile

Axl Protocol Music is not meant to be just another link hub. The purpose of this site is to give releases a place where story matters just as much as the audio itself.

Streaming services can tell people where a song lives. They rarely tell them what gave birth to it, what season of life shaped it, who helped carry it into existence, or why a release belongs to a larger arc.
""",
            NarrativeHighlights =
            [
                "release stories that go beyond title, date, and artwork",
                "credits that clearly name the people and roles behind each project",
                "lyrics and context that deepen how a listener hears the music",
                "artist history, milestones, and the evolving timeline of the project"
            ],
            OriginMarkdown = """
## A Label And An Artist, Still Intertwined

Right now, Axl Protocol Music exists to publish music explicitly for Axl Protocol. In practice, the artist identity and the label identity are still one and the same. That overlap is part of the story.

The long-term shape may grow larger than one artist, but this first chapter is intentionally centered on one voice, one catalog, and one world being built release by release.
""",
            Pillars =
            [
                new AboutPillar
                {
                    Title = "Story",
                    Description = "Releases are treated as chapters with emotional and personal context, not just isolated uploads."
                },
                new AboutPillar
                {
                    Title = "Identity",
                    Description = "Axl Protocol is a name, a perspective, and an evolving artistic world that ties songs together."
                },
                new AboutPillar
                {
                    Title = "Collaboration",
                    Description = "Credits, contributors, and personas matter. The work should reflect the people who shaped it."
                },
                new AboutPillar
                {
                    Title = "Continuity",
                    Description = "The site is designed so the catalog can grow into timelines, eras, archives, and future releases."
                }
            ]
        };
    }
}
