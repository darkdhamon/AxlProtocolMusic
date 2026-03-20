using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AxlProtocolMusic.WebApp.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("releases")]
public sealed class ReleasesController : Controller
{
    private readonly IReleaseService _releaseService;
    private readonly IImageStorageService _imageStorageService;

    public ReleasesController(
        IReleaseService releaseService,
        IImageStorageService imageStorageService)
    {
        _releaseService = releaseService;
        _imageStorageService = imageStorageService;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] ReleaseUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToCreate(
                request,
                GetFirstModelStateError() ?? "Complete all required release fields.");
        }

        if (request.CoverImageFile is not null)
        {
            try
            {
                var imageSaveResult = await _imageStorageService.SaveReleaseImageAsync(request.CoverImageFile);
                request.CoverImageUrl = imageSaveResult.Url;
            }
            catch (InvalidOperationException exception)
            {
                return RedirectToCreate(request, exception.Message);
            }
        }

        var result = await _releaseService.CreateReleaseAsync(request);
        if (!result.Succeeded)
        {
            return RedirectToCreate(request, result.ErrorMessage);
        }

        return Redirect($"/releases/{Uri.EscapeDataString(result.Slug)}?success=Release%20created.");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] ReleaseUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OriginalSlug))
        {
            return RedirectToDetails(request.Slug, "Original release slug is required.");
        }

        if (!ModelState.IsValid)
        {
            return RedirectToDetails(
                request.OriginalSlug,
                GetFirstModelStateError() ?? "Complete all required release fields.");
        }

        var previousCoverImageUrl = request.CoverImageUrl;

        if (request.CoverImageFile is not null)
        {
            try
            {
                var imageSaveResult = await _imageStorageService.SaveReleaseImageAsync(request.CoverImageFile);
                request.CoverImageUrl = imageSaveResult.Url;
            }
            catch (InvalidOperationException exception)
            {
                return RedirectToDetails(request.OriginalSlug, exception.Message);
            }
        }

        var result = await _releaseService.UpdateReleaseAsync(request);
        if (!result.Succeeded)
        {
            return RedirectToDetails(request.OriginalSlug, result.ErrorMessage);
        }

        if (request.CoverImageFile is not null
            && !string.IsNullOrWhiteSpace(previousCoverImageUrl)
            && _releaseService.IsManagedImageUrl(previousCoverImageUrl)
            && !string.Equals(previousCoverImageUrl, request.CoverImageUrl, StringComparison.OrdinalIgnoreCase))
        {
            await _imageStorageService.DeleteAsync(previousCoverImageUrl);
        }

        return Redirect($"/releases/{Uri.EscapeDataString(result.Slug)}?success=Release%20details%20updated.");
    }

    private RedirectResult RedirectToDetails(string slug, string errorMessage)
    {
        return Redirect($"/releases/{Uri.EscapeDataString(slug)}?error={Uri.EscapeDataString(errorMessage)}");
    }

    private string? GetFirstModelStateError()
    {
        return ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
    }

    private RedirectResult RedirectToCreate(ReleaseUpdateRequest request, string errorMessage)
    {
        var queryValues = new Dictionary<string, string?>
        {
            ["error"] = errorMessage,
            ["title"] = request.Title,
            ["slug"] = request.Slug,
            ["releaseDate"] = request.ReleaseDate == default
                ? null
                : request.ReleaseDate.ToString("yyyy-MM-dd"),
            ["coverImageUrl"] = request.CoverImageUrl,
            ["shortDescription"] = request.ShortDescription,
            ["isPublished"] = request.IsPublished.ToString()
        };

        return Redirect(QueryHelpers.AddQueryString("/releases/new", queryValues));
    }
}
