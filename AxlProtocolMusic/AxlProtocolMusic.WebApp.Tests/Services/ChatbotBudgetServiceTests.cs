using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class ChatbotBudgetServiceTests
{
    [Test]
    public async Task RecordUsageAsync_AccumulatesTokensAndEstimatedCost()
    {
        var stateRepository = new InMemoryRepository<ChatbotBudgetState>([]);
        var usageRepository = new InMemoryRepository<ChatbotUsageRecord>([]);
        var service = new ChatbotBudgetService(stateRepository, usageRepository);

        var summary = await service.RecordUsageAsync("gpt-5-mini", inputTokens: 1_000_000, outputTokens: 100_000, cachedInputTokens: 0);

        Assert.That(summary.TotalInputTokens, Is.EqualTo(1_000_000));
        Assert.That(summary.TotalOutputTokens, Is.EqualTo(100_000));
        Assert.That(summary.TotalRequestCount, Is.EqualTo(1));
        Assert.That(summary.TotalEstimatedCostUsd, Is.EqualTo(4.0m));
        Assert.That(summary.IsDisabled, Is.False);
        Assert.That(usageRepository.Documents, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RecordUsageAsync_DisablesChatbotWhenThresholdIsReached()
    {
        var stateRepository = new InMemoryRepository<ChatbotBudgetState>([]);
        var usageRepository = new InMemoryRepository<ChatbotUsageRecord>([]);
        var service = new ChatbotBudgetService(stateRepository, usageRepository);

        var summary = await service.RecordUsageAsync("gpt-5-mini", inputTokens: 1_000_000, outputTokens: 500_000, cachedInputTokens: 0);

        Assert.That(summary.TotalEstimatedCostUsd, Is.EqualTo(10.0m));
        Assert.That(summary.TotalRequestCount, Is.EqualTo(1));
        Assert.That(summary.IsDisabled, Is.True);
        Assert.That(summary.DisabledReason, Does.Contain("$10.00"));
        Assert.That(usageRepository.Documents.Single().TriggeredDisable, Is.True);
    }

    [Test]
    public async Task DisableForQuotaErrorAsync_DisablesUntilReset()
    {
        var stateRepository = new InMemoryRepository<ChatbotBudgetState>([]);
        var usageRepository = new InMemoryRepository<ChatbotUsageRecord>([]);
        var service = new ChatbotBudgetService(stateRepository, usageRepository);

        await service.DisableForQuotaErrorAsync("gpt-5-mini", "insufficient_quota", "Quota exceeded.");
        var disabledSummary = await service.GetSummaryAsync();

        Assert.That(disabledSummary.IsDisabled, Is.True);
        Assert.That(disabledSummary.TotalRequestCount, Is.EqualTo(1));
        Assert.That(disabledSummary.DisabledReason, Does.Contain("quota error"));

        await service.ResetAsync();
        var resetSummary = await service.GetSummaryAsync();

        Assert.That(resetSummary.IsDisabled, Is.False);
        Assert.That(resetSummary.TotalRequestCount, Is.EqualTo(0));
        Assert.That(resetSummary.TotalInputTokens, Is.EqualTo(0));
        Assert.That(resetSummary.TotalOutputTokens, Is.EqualTo(0));
        Assert.That(resetSummary.TotalEstimatedCostUsd, Is.EqualTo(0));
    }

    private sealed class InMemoryRepository<TDocument> : IRepository<TDocument>
        where TDocument : class, AxlProtocolMusic.WebApp.Models.IEntity
    {
        public InMemoryRepository(IEnumerable<TDocument> documents)
        {
            Documents = documents.ToList();
        }

        public List<TDocument> Documents { get; }

        public Task CreateAsync(TDocument document, CancellationToken cancellationToken = default)
        {
            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
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
            var existingIndex = Documents.FindIndex(item => string.Equals(item.Id, document.Id, StringComparison.Ordinal));
            if (existingIndex >= 0)
            {
                Documents[existingIndex] = document;
            }
            else
            {
                Documents.Add(document);
            }

            return Task.CompletedTask;
        }
    }
}
