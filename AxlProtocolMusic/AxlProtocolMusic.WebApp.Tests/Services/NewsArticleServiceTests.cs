using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Models;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class NewsArticleServiceTests
{
    [Test]
    public async Task GetArticlesAsync_WhenIncludeUnpublishedIsFalse_ReturnsOnlyPublishedArticlesNotScheduledOrderedDescending()
    {
        var now = DateTimeOffset.UtcNow;
        var repository = new InMemoryRepository<NewsArticle>(
        [
            CreateArticle("published-old", now.AddDays(-10), isPublished: true),
            CreateArticle("draft", now.AddDays(-2), isPublished: false),
            CreateArticle("published-new", now.AddDays(-1), isPublished: true),
            CreateArticle("scheduled", now.AddDays(2), isPublished: true)
        ]);

        var service = new NewsArticleService(repository);

        var result = await service.GetArticlesAsync(includeUnpublished: false);

        Assert.That(result.Select(item => item.Slug), Is.EqualTo(new[] { "published-new", "published-old" }));
    }

    [Test]
    public async Task GetArticlesAsync_WhenIncludeUnpublishedIsTrue_ReturnsAllArticlesOrderedDescending()
    {
        var now = DateTimeOffset.UtcNow;
        var repository = new InMemoryRepository<NewsArticle>(
        [
            CreateArticle("old", now.AddDays(-10), isPublished: true),
            CreateArticle("draft", now.AddDays(-2), isPublished: false),
            CreateArticle("scheduled", now.AddDays(2), isPublished: true)
        ]);

        var service = new NewsArticleService(repository);

        var result = await service.GetArticlesAsync(includeUnpublished: true);

        Assert.That(result.Select(item => item.Slug), Is.EqualTo(new[] { "scheduled", "draft", "old" }));
    }

    [Test]
    public async Task CreateAsync_NormalizesFieldsGeneratesIdAndCreatesUniqueSlug()
    {
        var repository = new InMemoryRepository<NewsArticle>(
        [
            CreateArticle("my-news-article", DateTimeOffset.UtcNow.AddDays(-5), isPublished: true),
            CreateArticle("my-news-article-2", DateTimeOffset.UtcNow.AddDays(-4), isPublished: true)
        ]);

        var service = new NewsArticleService(repository);

        var result = await service.CreateAsync(new NewsArticleUpdateRequest
        {
            Title = "  My News Article  ",
            Content = "  Content body  ",
            ImageUrl = "  /uploads/news/cover.png  ",
            PublicationDate = new DateTime(2026, 3, 21, 14, 45, 0),
            IsPublished = true,
            IsFeatured = true
        });

        Assert.That(result.Id, Is.Not.Empty);
        Assert.That(result.Title, Is.EqualTo("My News Article"));
        Assert.That(result.Slug, Is.EqualTo("my-news-article-3"));
        Assert.That(result.Content, Is.EqualTo("Content body"));
        Assert.That(result.ImageUrl, Is.EqualTo("/uploads/news/cover.png"));
        Assert.That(result.PublicationDateUtc, Is.EqualTo(new DateTimeOffset(new DateTime(2026, 3, 21), TimeSpan.Zero)));
        Assert.That(result.IsPublished, Is.True);
        Assert.That(result.IsFeatured, Is.True);
        Assert.That(repository.CreatedDocuments.Single(), Is.SameAs(result));
    }

    [Test]
    public async Task CreateAsync_WhenTitleNormalizesToEmptySlug_UsesFallbackSlugAndSuffix()
    {
        var repository = new InMemoryRepository<NewsArticle>(
        [
            CreateArticle("news-article", DateTimeOffset.UtcNow.AddDays(-2), isPublished: true)
        ]);

        var service = new NewsArticleService(repository);

        var result = await service.CreateAsync(new NewsArticleUpdateRequest
        {
            Title = " !!! ",
            Content = "Content",
            PublicationDate = new DateTime(2026, 3, 21)
        });

        Assert.That(result.Slug, Is.EqualTo("news-article-2"));
    }

    [Test]
    public void CreateAsync_WhenTitleIsBlank_ThrowsInvalidOperationException()
    {
        var service = new NewsArticleService(new InMemoryRepository<NewsArticle>([]));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CreateAsync(new NewsArticleUpdateRequest
        {
            Title = " ",
            Content = "Content",
            PublicationDate = new DateTime(2026, 3, 21)
        }));

        Assert.That(exception!.Message, Is.EqualTo("Title is required."));
    }

    [Test]
    public void CreateAsync_WhenContentIsBlank_ThrowsInvalidOperationException()
    {
        var service = new NewsArticleService(new InMemoryRepository<NewsArticle>([]));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CreateAsync(new NewsArticleUpdateRequest
        {
            Title = "Title",
            Content = " ",
            PublicationDate = new DateTime(2026, 3, 21)
        }));

        Assert.That(exception!.Message, Is.EqualTo("Content is required."));
    }

    [Test]
    public async Task UpdateAsync_WhenArticleExists_NormalizesAndPersistsChangesWithoutChangingSlug()
    {
        var existing = CreateArticle("original-slug", new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero), isPublished: false);
        existing.Id = "article-1";

        var repository = new InMemoryRepository<NewsArticle>([existing]);
        var service = new NewsArticleService(repository);

        var result = await service.UpdateAsync(new NewsArticleUpdateRequest
        {
            OriginalSlug = " ORIGINAL-SLUG ",
            Title = "  Updated Title  ",
            Slug = "ignored-by-service",
            Content = "  Updated content  ",
            ImageUrl = "  /uploads/news/updated.png  ",
            PublicationDate = new DateTime(2026, 3, 22, 7, 30, 0),
            IsPublished = true,
            IsFeatured = true
        });

        Assert.That(result, Is.SameAs(existing));
        Assert.That(result.Title, Is.EqualTo("Updated Title"));
        Assert.That(result.Slug, Is.EqualTo("original-slug"));
        Assert.That(result.Content, Is.EqualTo("Updated content"));
        Assert.That(result.ImageUrl, Is.EqualTo("/uploads/news/updated.png"));
        Assert.That(result.PublicationDateUtc, Is.EqualTo(new DateTimeOffset(new DateTime(2026, 3, 22), TimeSpan.Zero)));
        Assert.That(result.IsPublished, Is.True);
        Assert.That(result.IsFeatured, Is.True);
        Assert.That(repository.UpdatedDocuments.Single(), Is.SameAs(existing));
    }

    [Test]
    public void UpdateAsync_WhenOriginalSlugIsBlank_ThrowsInvalidOperationException()
    {
        var service = new NewsArticleService(new InMemoryRepository<NewsArticle>([]));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.UpdateAsync(new NewsArticleUpdateRequest
        {
            OriginalSlug = " ",
            Title = "Title",
            Content = "Content",
            PublicationDate = new DateTime(2026, 3, 21)
        }));

        Assert.That(exception!.Message, Is.EqualTo("The original article slug is required."));
    }

    [Test]
    public void UpdateAsync_WhenArticleDoesNotExist_ThrowsInvalidOperationException()
    {
        var service = new NewsArticleService(new InMemoryRepository<NewsArticle>([]));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.UpdateAsync(new NewsArticleUpdateRequest
        {
            OriginalSlug = "missing",
            Title = "Title",
            Content = "Content",
            PublicationDate = new DateTime(2026, 3, 21)
        }));

        Assert.That(exception!.Message, Is.EqualTo("The article could not be found."));
    }

    [Test]
    public async Task DeleteAsync_WhenIdIsValid_DeletesArticle()
    {
        var repository = new InMemoryRepository<NewsArticle>(
        [
            CreateArticle("one", DateTimeOffset.UtcNow.AddDays(-2), isPublished: true, id: "one"),
            CreateArticle("two", DateTimeOffset.UtcNow.AddDays(-1), isPublished: true, id: "two")
        ]);

        var service = new NewsArticleService(repository);

        await service.DeleteAsync("one");

        Assert.That(repository.DeletedIds, Is.EqualTo(new[] { "one" }));
        Assert.That(repository.Documents.Select(item => item.Id), Is.EqualTo(new[] { "two" }));
    }

    [Test]
    public void DeleteAsync_WhenIdIsBlank_ThrowsInvalidOperationException()
    {
        var service = new NewsArticleService(new InMemoryRepository<NewsArticle>([]));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.DeleteAsync(" "));

        Assert.That(exception!.Message, Is.EqualTo("The article id is required."));
    }

    [Test]
    public void IsManagedImageUrl_ReturnsTrueOnlyForUploadsPaths()
    {
        var service = new NewsArticleService(new InMemoryRepository<NewsArticle>([]));

        Assert.Multiple(() =>
        {
            Assert.That(service.IsManagedImageUrl("/uploads/news/image.png"), Is.True);
            Assert.That(service.IsManagedImageUrl("/UPLOADS/news/image.png"), Is.True);
            Assert.That(service.IsManagedImageUrl("/images/news/image.png"), Is.False);
            Assert.That(service.IsManagedImageUrl(""), Is.False);
            Assert.That(service.IsManagedImageUrl(null), Is.False);
        });
    }

    private static NewsArticle CreateArticle(string slug, DateTimeOffset publicationDateUtc, bool isPublished, string? id = null)
    {
        return new NewsArticle
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            Title = slug,
            Slug = slug,
            Content = $"{slug} content",
            ImageUrl = $"/images/{slug}.png",
            PublicationDateUtc = publicationDateUtc,
            IsPublished = isPublished
        };
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

        public List<string> DeletedIds { get; } = [];

        public Task CreateAsync(TDocument document, CancellationToken cancellationToken = default)
        {
            CreatedDocuments.Add(document);
            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            DeletedIds.Add(id);
            Documents.RemoveAll(document => string.Equals(document.Id, id, StringComparison.Ordinal));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TDocument>> FindAsync(Expression<Func<TDocument, bool>> filter, CancellationToken cancellationToken = default)
        {
            var predicate = filter.Compile();
            return Task.FromResult<IReadOnlyList<TDocument>>(Documents.Where(predicate).ToList());
        }

        public Task<IReadOnlyList<TDocument>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TDocument>>(Documents.ToList());

        public Task<TDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Documents.FirstOrDefault(document => string.Equals(document.Id, id, StringComparison.Ordinal)));

        public Task UpdateAsync(TDocument document, CancellationToken cancellationToken = default)
        {
            UpdatedDocuments.Add(document);
            return Task.CompletedTask;
        }
    }
}
