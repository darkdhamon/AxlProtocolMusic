using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AxlProtocolMusic.WebApp.Models.Content;

public sealed class ReleaseUpdateRequest
{
    public string? OriginalSlug { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string ShortDescription { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }

    public IFormFile? CoverImageFile { get; set; }

    public string? Story { get; set; }

    public string? Lyrics { get; set; }

    public List<ReleaseCredit> Credits { get; set; } = [];

    public List<ReleaseTrack> Tracks { get; set; } = [];

    public List<ReleaseLink> Links { get; set; } = [];

    public string? ReleaseTypeOverride { get; set; }

    public List<string> Tags { get; set; } = [];

    [Required]
    public DateTime ReleaseDate { get; set; }

    public bool IsPublished { get; set; }
}
