using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IMongoDbService
{
    Task<IReadOnlyList<TDocument>> GetAllAsync<TDocument>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity;

    Task<TDocument?> GetByIdAsync<TDocument>(
        string collectionName,
        string id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity;

    Task<IReadOnlyList<TDocument>> FilterByAsync<TDocument>(
        string collectionName,
        Expression<Func<TDocument, bool>> filter,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity;

    Task CreateAsync<TDocument>(
        string collectionName,
        TDocument document,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity;

    Task UpdateAsync<TDocument>(
        string collectionName,
        TDocument document,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity;

    Task DeleteAsync<TDocument>(
        string collectionName,
        string id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IEntity;
}
