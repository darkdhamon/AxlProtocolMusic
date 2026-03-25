using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models;
using AxlProtocolMusic.WebApp.Services;
using Microsoft.Extensions.Options;
using Moq;
using MongoDB.Driver;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class MongoDbServiceTests
{
    [Test]
    public void Constructor_WhenConnectionStringIsMissing_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new MongoDbService(
            Options.Create(new MongoDbSettings
            {
                ConnectionString = " ",
                DatabaseName = "AxlProtocolMusic"
            })));

        Assert.That(exception!.Message, Is.EqualTo("MongoDb:ConnectionString must be configured."));
    }

    [Test]
    public void Constructor_WhenDatabaseNameIsMissing_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new MongoDbService(
            Options.Create(new MongoDbSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = " "
            })));

        Assert.That(exception!.Message, Is.EqualTo("MongoDb:DatabaseName must be configured."));
    }

    [Test]
    public async Task GetAllAsync_UsesNamedCollectionAndReturnsAllDocuments()
    {
        var expected = new[]
        {
            new FakeEntity { Id = "one", Name = "First" },
            new FakeEntity { Id = "two", Name = "Second" }
        };

        FilterDefinition<FakeEntity>? capturedFilter = null;
        var collection = new Mock<IMongoCollection<FakeEntity>>();
        collection
            .Setup(item => item.FindAsync(
                It.IsAny<FilterDefinition<FakeEntity>>(),
                It.IsAny<FindOptions<FakeEntity, FakeEntity>>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<FakeEntity>, FindOptions<FakeEntity, FakeEntity>?, CancellationToken>((filter, _, _) => capturedFilter = filter)
            .ReturnsAsync(new TestAsyncCursor<FakeEntity>(expected));

        var database = new Mock<IMongoDatabase>();
        database
            .Setup(item => item.GetCollection<FakeEntity>("articles", It.IsAny<MongoCollectionSettings>()))
            .Returns(collection.Object);

        var service = CreateService(database.Object);

        var result = await service.GetAllAsync<FakeEntity>("articles");

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(capturedFilter, Is.SameAs(Builders<FakeEntity>.Filter.Empty));
        database.Verify(item => item.GetCollection<FakeEntity>("articles", It.IsAny<MongoCollectionSettings>()), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_FiltersByIdAndReturnsMatch()
    {
        var expected = new FakeEntity { Id = "entity-1", Name = "Matched" };
        FilterDefinition<FakeEntity>? capturedFilter = null;

        var collection = new Mock<IMongoCollection<FakeEntity>>();
        collection
            .Setup(item => item.FindAsync(
                It.IsAny<FilterDefinition<FakeEntity>>(),
                It.IsAny<FindOptions<FakeEntity, FakeEntity>>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<FakeEntity>, FindOptions<FakeEntity, FakeEntity>?, CancellationToken>((filter, _, _) => capturedFilter = filter)
            .ReturnsAsync(new TestAsyncCursor<FakeEntity>([expected]));

        var database = new Mock<IMongoDatabase>();
        database
            .Setup(item => item.GetCollection<FakeEntity>("entities", It.IsAny<MongoCollectionSettings>()))
            .Returns(collection.Object);

        var service = CreateService(database.Object);

        var result = await service.GetByIdAsync<FakeEntity>("entities", "entity-1");

        Assert.That(result, Is.EqualTo(expected));
        AssertMatchesIdFilter(capturedFilter, "entity-1");
    }

    [Test]
    public async Task FilterByAsync_ForwardsExpressionAndReturnsMatches()
    {
        var expected = new[]
        {
            new FakeEntity { Id = "two", Name = "Keep" }
        };

        FilterDefinition<FakeEntity>? capturedFilter = null;

        var collection = new Mock<IMongoCollection<FakeEntity>>();
        collection
            .Setup(item => item.FindAsync(
                It.IsAny<FilterDefinition<FakeEntity>>(),
                It.IsAny<FindOptions<FakeEntity, FakeEntity>>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<FakeEntity>, FindOptions<FakeEntity, FakeEntity>?, CancellationToken>((filter, _, _) => capturedFilter = filter)
            .ReturnsAsync(new TestAsyncCursor<FakeEntity>(expected));

        var database = new Mock<IMongoDatabase>();
        database
            .Setup(item => item.GetCollection<FakeEntity>("entities", It.IsAny<MongoCollectionSettings>()))
            .Returns(collection.Object);

        var service = CreateService(database.Object);

        var result = await service.FilterByAsync<FakeEntity>("entities", item => item.Name == "Keep");

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(capturedFilter, Is.TypeOf<ExpressionFilterDefinition<FakeEntity>>());
        var expression = ((ExpressionFilterDefinition<FakeEntity>)capturedFilter!).Expression.Compile();
        Assert.That(expression(new FakeEntity { Name = "Keep" }), Is.True);
        Assert.That(expression(new FakeEntity { Name = "Skip" }), Is.False);
    }

    [Test]
    public async Task CreateAsync_WhenIdIsBlank_AssignsNewIdBeforeInsert()
    {
        var entity = new FakeEntity { Name = "Created" };
        FakeEntity? inserted = null;

        var collection = new Mock<IMongoCollection<FakeEntity>>();
        collection
            .Setup(item => item.InsertOneAsync(It.IsAny<FakeEntity>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<FakeEntity, InsertOneOptions?, CancellationToken>((document, _, _) => inserted = document)
            .Returns(Task.CompletedTask);

        var database = new Mock<IMongoDatabase>();
        database
            .Setup(item => item.GetCollection<FakeEntity>("entities", It.IsAny<MongoCollectionSettings>()))
            .Returns(collection.Object);

        var service = CreateService(database.Object);

        await service.CreateAsync("entities", entity);

        Assert.That(entity.Id, Has.Length.EqualTo(32));
        Assert.That(inserted, Is.SameAs(entity));
    }

    [Test]
    public async Task CreateAsync_WhenIdAlreadyExists_PreservesExistingId()
    {
        var entity = new FakeEntity { Id = "existing-id", Name = "Created" };

        var collection = new Mock<IMongoCollection<FakeEntity>>();
        collection
            .Setup(item => item.InsertOneAsync(It.IsAny<FakeEntity>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var database = new Mock<IMongoDatabase>();
        database
            .Setup(item => item.GetCollection<FakeEntity>("entities", It.IsAny<MongoCollectionSettings>()))
            .Returns(collection.Object);

        var service = CreateService(database.Object);

        await service.CreateAsync("entities", entity);

        Assert.That(entity.Id, Is.EqualTo("existing-id"));
    }

    [Test]
    public async Task UpdateAsync_ReplacesByEntityIdWithoutUpsert()
    {
        var entity = new FakeEntity { Id = "entity-2", Name = "Updated" };
        FilterDefinition<FakeEntity>? capturedFilter = null;
        ReplaceOptions? capturedOptions = null;

        var collection = new Mock<IMongoCollection<FakeEntity>>();
        collection
            .Setup(item => item.ReplaceOneAsync(
                It.IsAny<FilterDefinition<FakeEntity>>(),
                It.IsAny<FakeEntity>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<FakeEntity>, FakeEntity, ReplaceOptions, CancellationToken>((filter, _, options, _) =>
            {
                capturedFilter = filter;
                capturedOptions = options;
            })
            .ReturnsAsync(Mock.Of<ReplaceOneResult>());

        var database = new Mock<IMongoDatabase>();
        database
            .Setup(item => item.GetCollection<FakeEntity>("entities", It.IsAny<MongoCollectionSettings>()))
            .Returns(collection.Object);

        var service = CreateService(database.Object);

        await service.UpdateAsync("entities", entity);

        AssertMatchesIdFilter(capturedFilter, "entity-2");
        Assert.That(capturedOptions, Is.Not.Null);
        Assert.That(capturedOptions!.IsUpsert, Is.False);
    }

    [Test]
    public async Task DeleteAsync_DeletesByEntityId()
    {
        FilterDefinition<FakeEntity>? capturedFilter = null;

        var collection = new Mock<IMongoCollection<FakeEntity>>();
        collection
            .Setup(item => item.DeleteOneAsync(It.IsAny<FilterDefinition<FakeEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<FakeEntity>, CancellationToken>((filter, _) => capturedFilter = filter)
            .ReturnsAsync(Mock.Of<DeleteResult>());

        var database = new Mock<IMongoDatabase>();
        database
            .Setup(item => item.GetCollection<FakeEntity>("entities", It.IsAny<MongoCollectionSettings>()))
            .Returns(collection.Object);

        var service = CreateService(database.Object);

        await service.DeleteAsync<FakeEntity>("entities", "entity-3");

        AssertMatchesIdFilter(capturedFilter, "entity-3");
    }

    private static MongoDbService CreateService(IMongoDatabase database)
    {
        var service = (MongoDbService)RuntimeHelpers.GetUninitializedObject(typeof(MongoDbService));
        typeof(MongoDbService)
            .GetField("_database", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(service, database);
        return service;
    }

    private static void AssertMatchesIdFilter(FilterDefinition<FakeEntity>? filter, string expectedId)
    {
        Assert.That(filter, Is.TypeOf<ExpressionFilterDefinition<FakeEntity>>());
        var expression = ((ExpressionFilterDefinition<FakeEntity>)filter!).Expression.Compile();
        Assert.That(expression(new FakeEntity { Id = expectedId }), Is.True);
        Assert.That(expression(new FakeEntity { Id = "other" }), Is.False);
    }

    private sealed class TestAsyncCursor<TDocument> : IAsyncCursor<TDocument>
    {
        private readonly IReadOnlyList<TDocument> _items;
        private bool _moved;

        public TestAsyncCursor(IReadOnlyList<TDocument> items)
        {
            _items = items;
        }

        public IEnumerable<TDocument> Current => _moved ? _items : [];

        public bool MoveNext(CancellationToken cancellationToken = default)
        {
            if (_moved)
            {
                return false;
            }

            _moved = true;
            return _items.Count > 0;
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(MoveNext(cancellationToken));

        public void Dispose()
        {
        }
    }

    public sealed class FakeEntity : IEntity
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}
