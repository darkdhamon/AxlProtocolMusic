using AxlProtocolMusic.WebApp.Models.Content;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface INewsArticleService
{
    Task<IReadOnlyList<NewsArticle>> GetArticlesAsync(
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default);

    Task<NewsArticle> CreateAsync(
        NewsArticleUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<NewsArticle> UpdateAsync(
        NewsArticleUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default);

    bool IsManagedImageUrl(string? imageUrl);
}
