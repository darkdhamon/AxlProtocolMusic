using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Models.Content;

public sealed class NewsArticle : IEntity
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public DateTimeOffset PublicationDateUtc { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }
}
