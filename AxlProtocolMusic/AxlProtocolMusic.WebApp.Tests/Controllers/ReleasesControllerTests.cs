using System.Text;
using AxlProtocolMusic.WebApp.Controllers;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Tests.Controllers;

[TestFixture]
public sealed class ReleasesControllerTests
{
    [Test]
    public async Task Create_WhenModelStateIsInvalid_RedirectsBackToCreateWithErrorAndInput()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("Title", "Title is required.");

        var request = new ReleaseUpdateRequest
        {
            Slug = "my-release",
            ShortDescription = "Short description",
            ReleaseDate = new DateTime(2026, 3, 1),
            IsPublished = true
        };

        var result = await controller.Create(request);

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult!.Url, Does.StartWith("/releases/new?"));
        Assert.That(redirectResult.Url, Does.Contain("error=Title%20is%20required."));
        Assert.That(redirectResult.Url, Does.Contain("slug=my-release"));
        Assert.That(redirectResult.Url, Does.Contain("releaseDate=2026-03-01"));
        Assert.That(redirectResult.Url, Does.Contain("isPublished=True"));
    }

    [Test]
    public async Task Create_WhenImageSaveSucceeds_PassesSavedImageUrlToReleaseServiceAndRedirectsToDetails()
    {
        var releaseService = new FakeReleaseService
        {
            CreateResult = new ReleaseCreateResult
            {
                Succeeded = true,
                Slug = "released-slug"
            }
        };

        var imageStorageService = new FakeImageStorageService
        {
            SaveResult = new ImageSaveResult
            {
                Url = "/images/releases/cover.png",
                StoragePath = "images/releases/cover.png"
            }
        };

        var controller = CreateController(releaseService, imageStorageService);
        var request = CreateValidRequest();
        request.CoverImageFile = CreateFormFile();

        var result = await controller.Create(request);

        Assert.That(imageStorageService.SavedFiles, Has.Count.EqualTo(1));
        Assert.That(releaseService.LastCreateRequest, Is.Not.Null);
        Assert.That(releaseService.LastCreateRequest!.CoverImageUrl, Is.EqualTo("/images/releases/cover.png"));

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult!.Url, Is.EqualTo("/releases/released-slug?success=Release%20created."));
    }

    [Test]
    public async Task Update_WhenOriginalSlugIsMissing_RedirectsToDetailsError()
    {
        var controller = CreateController();
        var request = CreateValidRequest();
        request.OriginalSlug = " ";

        var result = await controller.Update(request);

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(
            redirectResult!.Url,
            Is.EqualTo("/releases/valid-slug?error=Original%20release%20slug%20is%20required."));
    }

    [Test]
    public async Task Update_WhenManagedCoverImageChanges_DeletesPreviousImageAndRedirectsToUpdatedRelease()
    {
        var releaseService = new FakeReleaseService
        {
            UpdateResult = new ReleaseUpdateResult
            {
                Succeeded = true,
                Slug = "updated-slug"
            },
            ManagedImageUrl = true
        };

        var imageStorageService = new FakeImageStorageService
        {
            SaveResult = new ImageSaveResult
            {
                Url = "/images/releases/new-cover.png",
                StoragePath = "images/releases/new-cover.png"
            }
        };

        var controller = CreateController(releaseService, imageStorageService);
        var request = CreateValidRequest();
        request.OriginalSlug = "original-slug";
        request.CoverImageUrl = "/images/releases/old-cover.png";
        request.CoverImageFile = CreateFormFile();

        var result = await controller.Update(request);

        Assert.That(releaseService.LastUpdateRequest, Is.Not.Null);
        Assert.That(releaseService.LastUpdateRequest!.CoverImageUrl, Is.EqualTo("/images/releases/new-cover.png"));
        Assert.That(imageStorageService.DeletedPaths, Is.EqualTo(new[] { "/images/releases/old-cover.png" }));

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(
            redirectResult!.Url,
            Is.EqualTo("/releases/updated-slug?success=Release%20details%20updated."));
    }

    private static ReleasesController CreateController(
        FakeReleaseService? releaseService = null,
        FakeImageStorageService? imageStorageService = null)
    {
        return new ReleasesController(
            releaseService ?? new FakeReleaseService(),
            imageStorageService ?? new FakeImageStorageService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static ReleaseUpdateRequest CreateValidRequest()
    {
        return new ReleaseUpdateRequest
        {
            OriginalSlug = "valid-slug",
            Title = "Valid title",
            Slug = "valid-slug",
            ShortDescription = "Valid short description",
            ReleaseDate = new DateTime(2026, 3, 20),
            IsPublished = true
        };
    }

    private static IFormFile CreateFormFile()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("image-content"));
        return new FormFile(stream, 0, stream.Length, "coverImageFile", "cover.png");
    }

    private sealed class FakeReleaseService : IReleaseService
    {
        public ReleaseCreateResult CreateResult { get; set; } = new() { Succeeded = true, Slug = "created-slug" };

        public ReleaseUpdateResult UpdateResult { get; set; } = new() { Succeeded = true, Slug = "updated-slug" };

        public bool ManagedImageUrl { get; set; }

        public ReleaseUpdateRequest? LastCreateRequest { get; private set; }

        public ReleaseUpdateRequest? LastUpdateRequest { get; private set; }

        public Task<ReleaseCreateResult> CreateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
        {
            LastCreateRequest = request;
            return Task.FromResult(CreateResult);
        }

        public Task<ReleaseDeleteResult> DeleteReleaseAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<string> GenerateUniqueSlugAsync(string? value, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> GetKnownContributorNamesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> GetKnownCreditRolesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> GetKnownTagsAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PagedReleaseResult> GetPagedReleasesAsync(string? searchTerm, int pageNumber, int pageSize, bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ReleaseDetailsViewModel?> GetReleaseBySlugAsync(string slug, bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public bool IsManagedImageUrl(string? imageUrl) => ManagedImageUrl;

        public Task<ReleaseUpdateResult> UpdateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
        {
            LastUpdateRequest = request;
            return Task.FromResult(UpdateResult);
        }
    }

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public ImageSaveResult SaveResult { get; set; } = new();

        public List<string> DeletedPaths { get; } = [];

        public List<IFormFile> SavedFiles { get; } = [];

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            DeletedPaths.Add(storagePath);
            return Task.CompletedTask;
        }

        public bool IsManagedImageUrl(string? imageUrl)
            => !string.IsNullOrWhiteSpace(imageUrl) && imageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase);

        public Task<ImageSaveResult> SaveReleaseImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            SavedFiles.Add(file);
            return Task.FromResult(SaveResult);
        }
    }
}
