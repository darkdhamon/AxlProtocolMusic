using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Repositories.Interfaces;

public interface IRepository<TDocument>
    where TDocument : class, IEntity
{
    Task<IReadOnlyList<TDocument>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TDocument>> FindAsync(
        Expression<Func<TDocument, bool>> filter,
        CancellationToken cancellationToken = default);

    Task CreateAsync(TDocument document, CancellationToken cancellationToken = default);

    Task UpdateAsync(TDocument document, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
