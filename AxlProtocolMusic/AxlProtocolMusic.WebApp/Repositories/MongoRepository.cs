using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Models;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Interfaces;

namespace AxlProtocolMusic.WebApp.Repositories;

public class MongoRepository<TDocument> : IRepository<TDocument>
    where TDocument : class, IEntity
{
    private readonly IMongoDbService _mongoDbService;
    private readonly string _collectionName;

    public MongoRepository(IMongoDbService mongoDbService)
    {
        _mongoDbService = mongoDbService;
        _collectionName = typeof(TDocument).Name;
    }

    public Task<IReadOnlyList<TDocument>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _mongoDbService.GetAllAsync<TDocument>(_collectionName, cancellationToken);
    }

    public Task<TDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _mongoDbService.GetByIdAsync<TDocument>(_collectionName, id, cancellationToken);
    }

    public Task<IReadOnlyList<TDocument>> FindAsync(
        Expression<Func<TDocument, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return _mongoDbService.FilterByAsync(_collectionName, filter, cancellationToken);
    }

    public Task CreateAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        return _mongoDbService.CreateAsync(_collectionName, document, cancellationToken);
    }

    public Task UpdateAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        return _mongoDbService.UpdateAsync(_collectionName, document, cancellationToken);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return _mongoDbService.DeleteAsync<TDocument>(_collectionName, id, cancellationToken);
    }
}
