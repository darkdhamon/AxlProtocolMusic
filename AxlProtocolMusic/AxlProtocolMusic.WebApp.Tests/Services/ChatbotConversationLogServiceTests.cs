using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Models;
using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class ChatbotConversationLogServiceTests
{
    [Test]
    public async Task RecordAsync_PersistsTrimmedAnonymousConversationLog()
    {
        var repository = new InMemoryRepository<ChatbotConversationLogEntry>([]);
        var service = new ChatbotConversationLogService(repository);

        await service.RecordAsync(
            "  user question  ",
            "  assistant answer  ",
            " completed ",
            new ChatbotPageContext
            {
                PagePath = "  /releases/signals  ",
                PageTitle = "  Signals  "
            });

        Assert.That(repository.CreatedDocuments, Has.Count.EqualTo(1));

        var entry = repository.CreatedDocuments.Single();
        Assert.Multiple(() =>
        {
            Assert.That(entry.Id, Is.Not.Empty);
            Assert.That(entry.CreatedAtUtc, Is.Not.EqualTo(default(DateTimeOffset)));
            Assert.That(entry.UserMessage, Is.EqualTo("user question"));
            Assert.That(entry.AssistantReply, Is.EqualTo("assistant answer"));
            Assert.That(entry.Outcome, Is.EqualTo("completed"));
            Assert.That(entry.PagePath, Is.EqualTo("/releases/signals"));
            Assert.That(entry.PageTitle, Is.EqualTo("Signals"));
        });
    }

    [Test]
    public async Task RecordAsync_TruncatesOverlongValues()
    {
        var repository = new InMemoryRepository<ChatbotConversationLogEntry>([]);
        var service = new ChatbotConversationLogService(repository);

        await service.RecordAsync(
            new string('u', 4500),
            new string('a', 4500),
            new string('o', 120),
            new ChatbotPageContext
            {
                PagePath = "/" + new string('p', 350),
                PageTitle = new string('t', 350)
            });

        var entry = repository.CreatedDocuments.Single();
        Assert.Multiple(() =>
        {
            Assert.That(entry.UserMessage.Length, Is.EqualTo(4003));
            Assert.That(entry.UserMessage, Does.EndWith("..."));
            Assert.That(entry.AssistantReply.Length, Is.EqualTo(4003));
            Assert.That(entry.AssistantReply, Does.EndWith("..."));
            Assert.That(entry.Outcome.Length, Is.EqualTo(83));
            Assert.That(entry.PagePath.Length, Is.EqualTo(303));
            Assert.That(entry.PageTitle.Length, Is.EqualTo(303));
        });
    }

    [Test]
    public async Task GetRecentAsync_ReturnsNewestEntriesFirstAndClampsRequestedCount()
    {
        var entries = Enumerable.Range(1, 3)
            .Select(index => new ChatbotConversationLogEntry
            {
                Id = $"entry-{index}",
                CreatedAtUtc = new DateTimeOffset(2026, 4, index, 12, 0, 0, TimeSpan.Zero),
                UserMessage = $"message-{index}"
            })
            .ToList();

        var repository = new InMemoryRepository<ChatbotConversationLogEntry>(entries);
        var service = new ChatbotConversationLogService(repository);

        var result = await service.GetRecentAsync(500);

        Assert.That(result.Select(item => item.Id), Is.EqualTo(new[] { "entry-3", "entry-2", "entry-1" }));
    }

    [Test]
    public async Task GetExportAsync_ReturnsNewestEntriesFirstAndClampsRequestedCount()
    {
        var entries = Enumerable.Range(1, 3)
            .Select(index => new ChatbotConversationLogEntry
            {
                Id = $"entry-{index}",
                CreatedAtUtc = new DateTimeOffset(2026, 4, index, 12, 0, 0, TimeSpan.Zero),
                UserMessage = $"message-{index}"
            })
            .ToList();

        var repository = new InMemoryRepository<ChatbotConversationLogEntry>(entries);
        var service = new ChatbotConversationLogService(repository);

        var result = await service.GetExportAsync(0);

        Assert.That(result.Select(item => item.Id), Is.EqualTo(new[] { "entry-3" }));
    }

    [Test]
    public async Task RecordAsync_WhenValuesAreWhitespace_PersistsEmptyStrings()
    {
        var repository = new InMemoryRepository<ChatbotConversationLogEntry>([]);
        var service = new ChatbotConversationLogService(repository);

        await service.RecordAsync(" ", "\t", "\r\n", new ChatbotPageContext { PagePath = " ", PageTitle = null! });

        var entry = repository.CreatedDocuments.Single();
        Assert.Multiple(() =>
        {
            Assert.That(entry.UserMessage, Is.Empty);
            Assert.That(entry.AssistantReply, Is.Empty);
            Assert.That(entry.Outcome, Is.Empty);
            Assert.That(entry.PagePath, Is.Empty);
            Assert.That(entry.PageTitle, Is.Empty);
        });
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

        public Task<IReadOnlyList<TDocument>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TDocument>>(Documents.ToList());

        public Task<TDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Documents.FirstOrDefault());

        public Task<IReadOnlyList<TDocument>> FindAsync(Expression<Func<TDocument, bool>> filter, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TDocument>>([]);

        public Task CreateAsync(TDocument document, CancellationToken cancellationToken = default)
        {
            CreatedDocuments.Add(document);
            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TDocument document, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
