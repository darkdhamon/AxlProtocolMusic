using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using System.Text;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class NewsArticleService : INewsArticleService
{
    private readonly IRepository<NewsArticle> _newsArticleRepository;

    public NewsArticleService(IRepository<NewsArticle> newsArticleRepository)
    {
        _newsArticleRepository = newsArticleRepository;
    }

    public async Task<IReadOnlyList<NewsArticle>> GetArticlesAsync(
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default)
    {
        var articles = await _newsArticleRepository.GetAllAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        return articles
            .Where(article => includeUnpublished
                || (article.IsPublished && article.PublicationDateUtc <= now))
            .OrderByDescending(article => article.PublicationDateUtc)
            .ToList();
    }

    public async Task<NewsArticle> CreateAsync(
        NewsArticleUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var article = new NewsArticle
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = request.Title.Trim(),
            Slug = await GenerateUniqueSlugAsync(request.Title, cancellationToken),
            Content = request.Content.Trim(),
            ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
            PublicationDateUtc = new DateTimeOffset(DateTime.SpecifyKind(request.PublicationDate.Date, DateTimeKind.Utc)),
            IsPublished = request.IsPublished,
            IsFeatured = request.IsFeatured
        };

        await _newsArticleRepository.CreateAsync(article, cancellationToken);
        return article;
    }

    public async Task<NewsArticle> UpdateAsync(
        NewsArticleUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var originalSlug = request.OriginalSlug?.Trim();
        if (string.IsNullOrWhiteSpace(originalSlug))
        {
            throw new InvalidOperationException("The original article slug is required.");
        }

        var articles = await _newsArticleRepository.GetAllAsync(cancellationToken);
        var article = articles.FirstOrDefault(item =>
            string.Equals(item.Slug, originalSlug, StringComparison.OrdinalIgnoreCase));

        if (article is null)
        {
            throw new InvalidOperationException("The article could not be found.");
        }

        article.Title = request.Title.Trim();
        article.Content = request.Content.Trim();
        article.ImageUrl = request.ImageUrl?.Trim() ?? string.Empty;
        article.PublicationDateUtc = new DateTimeOffset(DateTime.SpecifyKind(request.PublicationDate.Date, DateTimeKind.Utc));
        article.IsPublished = request.IsPublished;
        article.IsFeatured = request.IsFeatured;

        await _newsArticleRepository.UpdateAsync(article, cancellationToken);
        return article;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("The article id is required.");
        }

        return _newsArticleRepository.DeleteAsync(id, cancellationToken);
    }

    public bool IsManagedImageUrl(string? imageUrl)
    {
        return !string.IsNullOrWhiteSpace(imageUrl)
            && imageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string? title,
        CancellationToken cancellationToken)
    {
        var baseSlug = NormalizeSlug(title);
        var articles = await _newsArticleRepository.GetAllAsync(cancellationToken);

        if (!articles.Any(article => string.Equals(article.Slug, baseSlug, StringComparison.OrdinalIgnoreCase)))
        {
            return baseSlug;
        }

        var suffix = 2;
        while (articles.Any(article => string.Equals(article.Slug, $"{baseSlug}-{suffix}", StringComparison.OrdinalIgnoreCase)))
        {
            suffix++;
        }

        return $"{baseSlug}-{suffix}";
    }

    private static void ValidateRequest(NewsArticleUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new InvalidOperationException("Content is required.");
        }
    }

    private static string NormalizeSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "news-article";
        }

        var slugBuilder = new StringBuilder();
        var previousWasHyphen = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                slugBuilder.Append(character);
                previousWasHyphen = false;
                continue;
            }

            if (character is ' ' or '-' or '_')
            {
                if (!previousWasHyphen && slugBuilder.Length > 0)
                {
                    slugBuilder.Append('-');
                    previousWasHyphen = true;
                }
            }
        }

        var slug = slugBuilder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "news-article" : slug;
    }
}
