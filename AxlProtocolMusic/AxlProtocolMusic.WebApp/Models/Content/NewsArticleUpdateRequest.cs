using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using AxlProtocolMusic.WebApp.Validation;

namespace AxlProtocolMusic.WebApp.Models.Content;

public sealed class NewsArticleUpdateRequest
{
    public string? OriginalSlug { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public IFormFile? ImageFile { get; set; }

    [NonDefaultDate(ErrorMessage = "Publication date is required.")]
    public DateTime PublicationDate { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }
}
