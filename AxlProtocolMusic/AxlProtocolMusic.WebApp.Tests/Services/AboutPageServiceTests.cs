using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Models;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class AboutPageServiceTests
{
    [Test]
    public async Task GetAsync_WhenContentExists_ReturnsStoredContent()
    {
        var existing = new AboutPageContent
        {
            Id = AboutPageContent.SingletonId,
            HeroLead = "Existing lead",
            HeroBody = "Existing body",
            FocusPoints = ["Focus"],
            WhyThisSiteExistsMarkdown = "Why",
            NarrativeHighlights = ["Highlight"],
            OriginMarkdown = "Origin",
            Pillars = [new AboutPillar { Title = "Story", Description = "Description" }]
        };

        var repository = new InMemoryRepository<AboutPageContent>([existing]);
        var service = new AboutPageService(repository);

        var result = await service.GetAsync();

        Assert.That(result, Is.SameAs(existing));
    }

    [Test]
    public async Task GetAsync_WhenContentDoesNotExist_ReturnsDefaultContent()
    {
        var service = new AboutPageService(new InMemoryRepository<AboutPageContent>([]));

        var result = await service.GetAsync();

        Assert.That(result.Id, Is.EqualTo(AboutPageContent.SingletonId));
        Assert.That(result.HeroLead, Does.StartWith("Axl Protocol is both the voice"));
        Assert.That(result.FocusPoints, Has.Count.EqualTo(3));
        Assert.That(result.NarrativeHighlights, Has.Count.EqualTo(4));
        Assert.That(result.Pillars, Has.Count.EqualTo(4));
        Assert.That(result.Pillars.Select(item => item.Title), Is.EqualTo(new[] { "Story", "Identity", "Collaboration", "Continuity" }));
    }

    [Test]
    public async Task UpdateAsync_WhenContentDoesNotExist_CreatesNormalizedSingletonContent()
    {
        var repository = new InMemoryRepository<AboutPageContent>([]);
        var service = new AboutPageService(repository);

        await service.UpdateAsync(new AboutPageContent
        {
            Id = "custom-id",
            HeroLead = "  Hero lead  ",
            HeroBody = "  Hero body  ",
            FocusPoints = [" Focus 1 ", " ", "Focus 2"],
            WhyThisSiteExistsMarkdown = "  Why markdown  ",
            NarrativeHighlights = [" Highlight 1 ", "", "Highlight 2 "],
            OriginMarkdown = "  Origin markdown  ",
            Pillars =
            [
                new AboutPillar { Title = " Story ", Description = " Description " },
                new AboutPillar { Title = " ", Description = " " }
            ]
        });

        Assert.That(repository.CreatedDocuments, Has.Count.EqualTo(1));
        var created = repository.CreatedDocuments.Single();

        Assert.That(created.Id, Is.EqualTo(AboutPageContent.SingletonId));
        Assert.That(created.HeroLead, Is.EqualTo("Hero lead"));
        Assert.That(created.HeroBody, Is.EqualTo("Hero body"));
        Assert.That(created.FocusPoints, Is.EqualTo(new[] { "Focus 1", "Focus 2" }));
        Assert.That(created.WhyThisSiteExistsMarkdown, Is.EqualTo("Why markdown"));
        Assert.That(created.NarrativeHighlights, Is.EqualTo(new[] { "Highlight 1", "Highlight 2" }));
        Assert.That(created.OriginMarkdown, Is.EqualTo("Origin markdown"));
        Assert.That(created.Pillars, Has.Count.EqualTo(1));
        Assert.That(created.Pillars[0].Title, Is.EqualTo("Story"));
        Assert.That(created.Pillars[0].Description, Is.EqualTo("Description"));
    }

    [Test]
    public async Task UpdateAsync_WhenContentExists_UpdatesNormalizedSingletonContent()
    {
        var existing = new AboutPageContent
        {
            Id = AboutPageContent.SingletonId,
            HeroLead = "Old lead"
        };

        var repository = new InMemoryRepository<AboutPageContent>([existing]);
        var service = new AboutPageService(repository);

        await service.UpdateAsync(new AboutPageContent
        {
            HeroLead = "  New lead  ",
            HeroBody = "  New body  ",
            FocusPoints = [" New focus "],
            WhyThisSiteExistsMarkdown = "  Why  ",
            NarrativeHighlights = [" Highlight "],
            OriginMarkdown = "  Origin  ",
            Pillars = [new AboutPillar { Title = " Identity ", Description = " Value " }]
        });

        Assert.That(repository.CreatedDocuments, Is.Empty);
        Assert.That(repository.UpdatedDocuments, Has.Count.EqualTo(1));

        var updated = repository.UpdatedDocuments.Single();
        Assert.That(updated.Id, Is.EqualTo(AboutPageContent.SingletonId));
        Assert.That(updated.HeroLead, Is.EqualTo("New lead"));
        Assert.That(updated.HeroBody, Is.EqualTo("New body"));
        Assert.That(updated.FocusPoints, Is.EqualTo(new[] { "New focus" }));
        Assert.That(updated.WhyThisSiteExistsMarkdown, Is.EqualTo("Why"));
        Assert.That(updated.NarrativeHighlights, Is.EqualTo(new[] { "Highlight" }));
        Assert.That(updated.OriginMarkdown, Is.EqualTo("Origin"));
        Assert.That(updated.Pillars[0].Title, Is.EqualTo("Identity"));
        Assert.That(updated.Pillars[0].Description, Is.EqualTo("Value"));
    }

    [Test]
    public async Task SeedAsync_WhenContentDoesNotExist_CreatesDefaultContent()
    {
        var repository = new InMemoryRepository<AboutPageContent>([]);
        var service = new AboutPageService(repository);

        await service.SeedAsync();

        Assert.That(repository.CreatedDocuments, Has.Count.EqualTo(1));
        Assert.That(repository.CreatedDocuments.Single().Id, Is.EqualTo(AboutPageContent.SingletonId));
    }

    [Test]
    public async Task SeedAsync_WhenContentExists_DoesNothing()
    {
        var repository = new InMemoryRepository<AboutPageContent>(
        [
            new AboutPageContent { Id = AboutPageContent.SingletonId, HeroLead = "Existing" }
        ]);

        var service = new AboutPageService(repository);

        await service.SeedAsync();

        Assert.That(repository.CreatedDocuments, Is.Empty);
    }

    private sealed class InMemoryRepository<TDocument> : IRepository<TDocument>
        where TDocument : class, IEntity
    {
        public InMemoryRepository(IEnumerable<TDocument> documents)
        {
            Documents = documents.ToList();
        }

        public List<TDocument> Documents { get; }

        public List<TDocument> CreatedDocuments { get; } = [];

        public List<TDocument> UpdatedDocuments { get; } = [];

        public Task<IReadOnlyList<TDocument>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TDocument>>(Documents.ToList());

        public Task<TDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Documents.FirstOrDefault(document => string.Equals(document.Id, id, StringComparison.Ordinal)));

        public Task<IReadOnlyList<TDocument>> FindAsync(Expression<Func<TDocument, bool>> filter, CancellationToken cancellationToken = default)
        {
            var predicate = filter.Compile();
            return Task.FromResult<IReadOnlyList<TDocument>>(Documents.Where(predicate).ToList());
        }

        public Task CreateAsync(TDocument document, CancellationToken cancellationToken = default)
        {
            CreatedDocuments.Add(document);
            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TDocument document, CancellationToken cancellationToken = default)
        {
            UpdatedDocuments.Add(document);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            Documents.RemoveAll(document => string.Equals(document.Id, id, StringComparison.Ordinal));
            return Task.CompletedTask;
        }
    }
}
