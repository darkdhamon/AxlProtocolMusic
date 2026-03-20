using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class MongoDbService : IMongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IOptions<MongoDbSettings> settings)
    {
        var mongoSettings = settings.Value;

        if (string.IsNullOrWhiteSpace(mongoSettings.ConnectionString))
        {
            throw new InvalidOperationException("MongoDb:ConnectionString must be configured.");
        }

        if (string.IsNullOrWhiteSpace(mongoSettings.DatabaseName))
        {
            throw new InvalidOperationException("MongoDb:DatabaseName must be configured.");
        }

        var client = new MongoClient(mongoSettings.ConnectionString);
        _database = client.GetDatabase(mongoSettings.DatabaseName);
    }

    public async Task<IReadOnlyList<TDocument>> GetAllAsync<TDocument>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity
    {
        return await GetCollection<TDocument>(collectionName)
            .Find(Builders<TDocument>.Filter.Empty)
            .ToListAsync(cancellationToken);
    }

    public async Task<TDocument?> GetByIdAsync<TDocument>(
        string collectionName,
        string id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity
    {
        return await GetCollection<TDocument>(collectionName)
            .Find(document => document.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TDocument>> FilterByAsync<TDocument>(
        string collectionName,
        Expression<Func<TDocument, bool>> filter,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity
    {
        return await GetCollection<TDocument>(collectionName)
            .Find(filter)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync<TDocument>(
        string collectionName,
        TDocument document,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity
    {
        if (string.IsNullOrWhiteSpace(document.Id))
        {
            document.Id = Guid.NewGuid().ToString("N");
        }

        await GetCollection<TDocument>(collectionName)
            .InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync<TDocument>(
        string collectionName,
        TDocument document,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity
    {
        await GetCollection<TDocument>(collectionName)
            .ReplaceOneAsync(
                existingDocument => existingDocument.Id == document.Id,
                document,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken);
    }

    public async Task DeleteAsync<TDocument>(
        string collectionName,
        string id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity
    {
        await GetCollection<TDocument>(collectionName)
            .DeleteOneAsync(document => document.Id == id, cancellationToken);
    }

    private IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName)
        where TDocument : class
    {
        return _database.GetCollection<TDocument>(collectionName);
    }
}
