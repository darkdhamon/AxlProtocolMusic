using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models;
using AxlProtocolMusic.WebApp.Services;
using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Driver;

namespace AxlProtocolMusic.WebApp.IntegrationTests.Services;

[TestFixture]
[NonParallelizable]
public sealed class MongoDbServiceIntegrationTests
{
    private MongoDbRunner? _runner;
    private MongoClient? _client;
    private MongoDbService? _service;
    private string _databaseName = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _runner = MongoDbRunner.Start();
        _client = new MongoClient(_runner.ConnectionString);
        _databaseName = $"AxlProtocolMusicIntegration_{Guid.NewGuid():N}";
        _service = new MongoDbService(
            Options.Create(new MongoDbSettings
            {
                ConnectionString = _runner.ConnectionString,
                DatabaseName = _databaseName
            }));
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_client is not null && !string.IsNullOrWhiteSpace(_databaseName))
        {
            await _client.DropDatabaseAsync(_databaseName);
        }

        (_client as IDisposable)?.Dispose();
        _runner?.Dispose();
        _service = null;
        _client = null;
        _runner = null;
    }

    [Test]
    public async Task CreateAndQueryAsync_PersistsAndReadsDocumentsFromRealMongo()
    {
        var first = new IntegrationEntity { Name = "First", Category = "alpha" };
        var second = new IntegrationEntity { Name = "Second", Category = "beta" };

        await Service.CreateAsync(CollectionName, first);
        await Service.CreateAsync(CollectionName, second);

        var all = await Service.GetAllAsync<IntegrationEntity>(CollectionName);
        var byId = await Service.GetByIdAsync<IntegrationEntity>(CollectionName, first.Id);
        var filtered = await Service.FilterByAsync<IntegrationEntity>(CollectionName, item => item.Category == "beta");

        Assert.That(first.Id, Has.Length.EqualTo(32));
        Assert.That(second.Id, Has.Length.EqualTo(32));
        Assert.That(all.Select(item => item.Name), Is.EquivalentTo(["First", "Second"]));
        Assert.That(byId, Is.Not.Null);
        Assert.That(byId!.Name, Is.EqualTo("First"));
        Assert.That(filtered.Select(item => item.Name), Is.EqualTo(["Second"]));
    }

    [Test]
    public async Task UpdateAndDeleteAsync_ModifiesAndRemovesDocumentsInRealMongo()
    {
        var entity = new IntegrationEntity { Name = "Original", Category = "alpha" };
        await Service.CreateAsync(CollectionName, entity);

        entity.Name = "Updated";
        entity.Category = "gamma";
        await Service.UpdateAsync(CollectionName, entity);

        var updated = await Service.GetByIdAsync<IntegrationEntity>(CollectionName, entity.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Name, Is.EqualTo("Updated"));
        Assert.That(updated.Category, Is.EqualTo("gamma"));

        await Service.DeleteAsync<IntegrationEntity>(CollectionName, entity.Id);

        var afterDelete = await Service.GetByIdAsync<IntegrationEntity>(CollectionName, entity.Id);
        var remaining = await Service.GetAllAsync<IntegrationEntity>(CollectionName);

        Assert.That(afterDelete, Is.Null);
        Assert.That(remaining, Is.Empty);
    }

    private MongoDbService Service => _service ?? throw new InvalidOperationException("Service was not initialized.");

    private const string CollectionName = "integration-entities";

    private sealed class IntegrationEntity : IEntity
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
    }
}
