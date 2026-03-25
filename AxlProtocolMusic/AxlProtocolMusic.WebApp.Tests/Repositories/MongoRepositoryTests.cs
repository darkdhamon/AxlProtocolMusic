using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Models;
using AxlProtocolMusic.WebApp.Repositories;
using AxlProtocolMusic.WebApp.Services.Interfaces;

namespace AxlProtocolMusic.WebApp.Tests.Repositories;

[TestFixture]
public sealed class MongoRepositoryTests
{
    [Test]
    public async Task GetAllAsync_UsesDocumentTypeNameAsCollectionAndForwardsCancellationToken()
    {
        var expected = new[]
        {
            new FakeDocument { Id = "one", Name = "First" },
            new FakeDocument { Id = "two", Name = "Second" }
        };

        var mongoDbService = new FakeMongoDbService
        {
            GetAllResult = expected
        };

        var repository = new MongoRepository<FakeDocument>(mongoDbService);
        using var cancellationSource = new CancellationTokenSource();

        var result = await repository.GetAllAsync(cancellationSource.Token);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(mongoDbService.LastCollectionName, Is.EqualTo("FakeDocument"));
        Assert.That(mongoDbService.LastCall, Is.EqualTo("GetAll"));
        Assert.That(mongoDbService.LastCancellationToken, Is.EqualTo(cancellationSource.Token));
    }

    [Test]
    public async Task GetByIdAsync_ForwardsIdAndCancellationToken()
    {
        var expected = new FakeDocument { Id = "doc-1", Name = "Expected" };
        var mongoDbService = new FakeMongoDbService
        {
            GetByIdResult = expected
        };

        var repository = new MongoRepository<FakeDocument>(mongoDbService);
        using var cancellationSource = new CancellationTokenSource();

        var result = await repository.GetByIdAsync("doc-1", cancellationSource.Token);

        Assert.That(result, Is.SameAs(expected));
        Assert.That(mongoDbService.LastCollectionName, Is.EqualTo("FakeDocument"));
        Assert.That(mongoDbService.LastId, Is.EqualTo("doc-1"));
        Assert.That(mongoDbService.LastCall, Is.EqualTo("GetById"));
        Assert.That(mongoDbService.LastCancellationToken, Is.EqualTo(cancellationSource.Token));
    }

    [Test]
    public async Task FindAsync_ForwardsExpressionAndCancellationToken()
    {
        var expected = new[]
        {
            new FakeDocument { Id = "match", Name = "Target" }
        };

        var mongoDbService = new FakeMongoDbService
        {
            FilterResult = expected
        };

        var repository = new MongoRepository<FakeDocument>(mongoDbService);
        Expression<Func<FakeDocument, bool>> filter = document => document.Name == "Target";
        using var cancellationSource = new CancellationTokenSource();

        var result = await repository.FindAsync(filter, cancellationSource.Token);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(mongoDbService.LastCollectionName, Is.EqualTo("FakeDocument"));
        Assert.That(mongoDbService.LastFilter, Is.SameAs(filter));
        Assert.That(mongoDbService.LastCall, Is.EqualTo("FilterBy"));
        Assert.That(mongoDbService.LastCancellationToken, Is.EqualTo(cancellationSource.Token));
    }

    [Test]
    public async Task CreateAsync_ForwardsDocumentAndCancellationToken()
    {
        var mongoDbService = new FakeMongoDbService();
        var repository = new MongoRepository<FakeDocument>(mongoDbService);
        var document = new FakeDocument { Id = "doc-1", Name = "Created" };
        using var cancellationSource = new CancellationTokenSource();

        await repository.CreateAsync(document, cancellationSource.Token);

        Assert.That(mongoDbService.LastCollectionName, Is.EqualTo("FakeDocument"));
        Assert.That(mongoDbService.LastDocument, Is.SameAs(document));
        Assert.That(mongoDbService.LastCall, Is.EqualTo("Create"));
        Assert.That(mongoDbService.LastCancellationToken, Is.EqualTo(cancellationSource.Token));
    }

    [Test]
    public async Task UpdateAsync_ForwardsDocumentAndCancellationToken()
    {
        var mongoDbService = new FakeMongoDbService();
        var repository = new MongoRepository<FakeDocument>(mongoDbService);
        var document = new FakeDocument { Id = "doc-2", Name = "Updated" };
        using var cancellationSource = new CancellationTokenSource();

        await repository.UpdateAsync(document, cancellationSource.Token);

        Assert.That(mongoDbService.LastCollectionName, Is.EqualTo("FakeDocument"));
        Assert.That(mongoDbService.LastDocument, Is.SameAs(document));
        Assert.That(mongoDbService.LastCall, Is.EqualTo("Update"));
        Assert.That(mongoDbService.LastCancellationToken, Is.EqualTo(cancellationSource.Token));
    }

    [Test]
    public async Task DeleteAsync_ForwardsIdAndCancellationToken()
    {
        var mongoDbService = new FakeMongoDbService();
        var repository = new MongoRepository<FakeDocument>(mongoDbService);
        using var cancellationSource = new CancellationTokenSource();

        await repository.DeleteAsync("doc-3", cancellationSource.Token);

        Assert.That(mongoDbService.LastCollectionName, Is.EqualTo("FakeDocument"));
        Assert.That(mongoDbService.LastId, Is.EqualTo("doc-3"));
        Assert.That(mongoDbService.LastCall, Is.EqualTo("Delete"));
        Assert.That(mongoDbService.LastCancellationToken, Is.EqualTo(cancellationSource.Token));
    }

    private sealed class FakeDocument : IEntity
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed class FakeMongoDbService : IMongoDbService
    {
        public IReadOnlyList<FakeDocument> GetAllResult { get; set; } = [];

        public FakeDocument? GetByIdResult { get; set; }

        public IReadOnlyList<FakeDocument> FilterResult { get; set; } = [];

        public string LastCollectionName { get; private set; } = string.Empty;

        public string LastId { get; private set; } = string.Empty;

        public string LastCall { get; private set; } = string.Empty;

        public CancellationToken LastCancellationToken { get; private set; }

        public FakeDocument? LastDocument { get; private set; }

        public Expression<Func<FakeDocument, bool>>? LastFilter { get; private set; }

        public Task<IReadOnlyList<TDocument>> GetAllAsync<TDocument>(string collectionName, CancellationToken cancellationToken = default)
            where TDocument : class, IEntity
        {
            LastCall = "GetAll";
            LastCollectionName = collectionName;
            LastCancellationToken = cancellationToken;
            return Task.FromResult((IReadOnlyList<TDocument>)GetAllResult);
        }

        public Task<TDocument?> GetByIdAsync<TDocument>(string collectionName, string id, CancellationToken cancellationToken = default)
            where TDocument : class, IEntity
        {
            LastCall = "GetById";
            LastCollectionName = collectionName;
            LastId = id;
            LastCancellationToken = cancellationToken;
            return Task.FromResult((TDocument?)(object?)GetByIdResult);
        }

        public Task<IReadOnlyList<TDocument>> FilterByAsync<TDocument>(string collectionName, Expression<Func<TDocument, bool>> filter, CancellationToken cancellationToken = default)
            where TDocument : class, IEntity
        {
            LastCall = "FilterBy";
            LastCollectionName = collectionName;
            LastCancellationToken = cancellationToken;
            LastFilter = (Expression<Func<FakeDocument, bool>>)(object)filter;
            return Task.FromResult((IReadOnlyList<TDocument>)FilterResult);
        }

        public Task CreateAsync<TDocument>(string collectionName, TDocument document, CancellationToken cancellationToken = default)
            where TDocument : class, IEntity
        {
            LastCall = "Create";
            LastCollectionName = collectionName;
            LastCancellationToken = cancellationToken;
            LastDocument = (FakeDocument)(object)document;
            return Task.CompletedTask;
        }

        public Task UpdateAsync<TDocument>(string collectionName, TDocument document, CancellationToken cancellationToken = default)
            where TDocument : class, IEntity
        {
            LastCall = "Update";
            LastCollectionName = collectionName;
            LastCancellationToken = cancellationToken;
            LastDocument = (FakeDocument)(object)document;
            return Task.CompletedTask;
        }

        public Task DeleteAsync<TDocument>(string collectionName, string id, CancellationToken cancellationToken = default)
            where TDocument : class, IEntity
        {
            LastCall = "Delete";
            LastCollectionName = collectionName;
            LastId = id;
            LastCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
