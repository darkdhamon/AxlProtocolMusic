using System.Text;
using AxlProtocolMusic.WebApp.Controllers;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel.DataAnnotations;

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
    public async Task Create_WhenReleaseDateIsMissing_RedirectsBackToCreateWithDateValidationError()
    {
        var controller = CreateController();
        var request = new ReleaseUpdateRequest
        {
            Title = "My Release",
            Slug = "my-release",
            ShortDescription = "Short description"
        };
        AddValidationErrors(controller, request);

        var result = await controller.Create(request);

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult!.Url, Does.StartWith("/releases/new?"));
        Assert.That(redirectResult.Url, Does.Contain("error=Release%20date%20is%20required."));
        Assert.That(redirectResult.Url, Does.Not.Contain("releaseDate="));
    }

    [Test]
    public async Task Create_WhenImageSaveFails_RedirectsBackToCreateWithErrorAndInput()
    {
        var releaseService = new FakeReleaseService();
        var imageStorageService = new FakeImageStorageService
        {
            ThrowOnSave = new InvalidOperationException("Image format is not supported.")
        };

        var request = CreateValidRequest();
        request.CoverImageFile = CreateFormFile();
        request.CoverImageUrl = "/uploaded/previous-image.png";

        var controller = CreateController(releaseService, imageStorageService);

        var result = await controller.Create(request);

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult!.Url, Does.StartWith("/releases/new?"));
        Assert.That(redirectResult.Url, Does.Contain("error=Image%20format%20is%20not%20supported."));
        Assert.That(redirectResult.Url, Does.Contain("slug=valid-slug"));
        Assert.That(redirectResult.Url, Does.Contain("releaseDate=2026-03-20"));
        Assert.That(redirectResult.Url, Does.Contain("coverImageUrl=%2Fuploaded%2Fprevious-image.png"));
        Assert.That(redirectResult.Url, Does.Contain("isPublished=True"));
        Assert.That(releaseService.LastCreateRequest, Is.Null);
        Assert.That(imageStorageService.SavedFiles, Is.Empty);
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
    public async Task Update_WhenReleaseDateIsMissing_RedirectsToDetailsWithDateValidationError()
    {
        var controller = CreateController();
        var request = CreateValidRequest();
        request.OriginalSlug = "original-slug";
        request.ReleaseDate = default;
        AddValidationErrors(controller, request);

        var result = await controller.Update(request);

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(
            redirectResult!.Url,
            Is.EqualTo("/releases/original-slug?error=Release%20date%20is%20required."));
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
            }
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

    [Test]
    public async Task Create_WhenCreateFailsAfterImageSave_DeletesUploadedImageAndRedirectsToCreate()
    {
        var releaseService = new FakeReleaseService
        {
            CreateResult = new ReleaseCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Could not save release."
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
        request.CoverImageUrl = "/images/releases/original-cover.png";
        request.CoverImageFile = CreateFormFile();

        var result = await controller.Create(request);

        Assert.That(imageStorageService.DeletedPaths, Is.EqualTo(new[] { "images/releases/cover.png" }));
        Assert.That(imageStorageService.SavedFiles, Has.Count.EqualTo(1));

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(
            redirectResult!.Url,
            Does.StartWith("/releases/new?"));
        Assert.That(redirectResult!.Url, Does.Contain("error=Could%20not%20save%20release."));
        Assert.That(redirectResult.Url, Does.Contain("coverImageUrl=%2Fimages%2Freleases%2Foriginal-cover.png"));
        Assert.That(redirectResult.Url, Does.Not.Contain("coverImageUrl=%2Fimages%2Freleases%2Fcover.png"));
    }

    [Test]
    public async Task Create_WhenRollbackDeleteFails_RedirectsWithOriginalFailure()
    {
        var releaseService = new FakeReleaseService
        {
            CreateResult = new ReleaseCreateResult { Succeeded = false, ErrorMessage = "Slug already exists." }
        };
        var imageStorageService = new FakeImageStorageService
        {
            SaveResult = new ImageSaveResult
            {
                Url = "/images/releases/cover.png",
                StoragePath = "images/releases/cover.png"
            },
            DeleteException = new IOException("Storage unavailable.")
        };
        var request = CreateValidRequest();
        request.CoverImageFile = CreateFormFile();

        var result = await CreateController(releaseService, imageStorageService).Create(request);

        Assert.That(result, Is.TypeOf<RedirectResult>());
        Assert.That(((RedirectResult)result).Url, Does.Contain("error=Slug%20already%20exists."));
        Assert.That(((RedirectResult)result).Url, Does.Not.Contain("coverImageUrl="));
    }

    [Test]
    public async Task Update_WhenUpdateFailsAfterImageSave_DeletesUploadedImageAndRedirectsToDetails()
    {
        var releaseService = new FakeReleaseService
        {
            UpdateResult = new ReleaseUpdateResult
            {
                Succeeded = false,
                ErrorMessage = "Could not update release."
            }
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

        Assert.That(imageStorageService.DeletedPaths, Is.EqualTo(new[] { "images/releases/new-cover.png" }));
        Assert.That(releaseService.LastUpdateRequest, Is.Not.Null);
        Assert.That(releaseService.LastUpdateRequest!.CoverImageUrl, Is.EqualTo("/images/releases/new-cover.png"));

        var redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(
            redirectResult!.Url,
            Is.EqualTo("/releases/original-slug?error=Could%20not%20update%20release."));
    }

    [Test]
    public async Task Update_WhenRollbackDeleteFails_RedirectsWithOriginalFailure()
    {
        var releaseService = new FakeReleaseService
        {
            UpdateResult = new ReleaseUpdateResult { Succeeded = false, ErrorMessage = "Slug already exists." }
        };
        var imageStorageService = new FakeImageStorageService
        {
            SaveResult = new ImageSaveResult
            {
                Url = "/images/releases/new-cover.png",
                StoragePath = "images/releases/new-cover.png"
            },
            DeleteException = new IOException("Storage unavailable.")
        };
        var request = CreateValidRequest();
        request.OriginalSlug = "original-slug";
        request.CoverImageFile = CreateFormFile();

        var result = await CreateController(releaseService, imageStorageService).Update(request);

        Assert.That(result, Is.TypeOf<RedirectResult>());
        Assert.That(
            ((RedirectResult)result).Url,
            Is.EqualTo("/releases/original-slug?error=Slug%20already%20exists."));
    }

    private static ReleasesController CreateController(
        FakeReleaseService? releaseService = null,
        FakeImageStorageService? imageStorageService = null)
    {
        return new ReleasesController(
            releaseService ?? new FakeReleaseService(),
            imageStorageService ?? new FakeImageStorageService(),
            NullLogger<ReleasesController>.Instance)
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

    private static void AddValidationErrors(ReleasesController controller, object model)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        foreach (var validationResult in validationResults)
        {
            var memberName = validationResult.MemberNames.FirstOrDefault() ?? string.Empty;
            controller.ModelState.AddModelError(memberName, validationResult.ErrorMessage ?? "Invalid value.");
        }
    }

    private sealed class FakeReleaseService : IReleaseService
    {
        public ReleaseCreateResult CreateResult { get; set; } = new() { Succeeded = true, Slug = "created-slug" };

        public ReleaseUpdateResult UpdateResult { get; set; } = new() { Succeeded = true, Slug = "updated-slug" };

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

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetKnownContributorRolesByNameAsync(CancellationToken cancellationToken = default)
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

        public Task<ReleaseUpdateResult> UpdateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
        {
            LastUpdateRequest = request;
            return Task.FromResult(UpdateResult);
        }
    }

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public ImageSaveResult SaveResult { get; set; } = new();
        public Exception? ThrowOnSave { get; set; }

        public List<string> DeletedPaths { get; } = [];

        public List<IFormFile> SavedFiles { get; } = [];

        public Exception? DeleteException { get; set; }

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            DeletedPaths.Add(storagePath);
            if (DeleteException is not null)
            {
                return Task.FromException(DeleteException);
            }

            return Task.CompletedTask;
        }

        public bool IsManagedImageUrl(string? imageUrl)
            => !string.IsNullOrWhiteSpace(imageUrl) && imageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase);

        public Task<ImageSaveResult> SaveReleaseImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSave is not null)
            {
                throw ThrowOnSave;
            }

            SavedFiles.Add(file);
            return Task.FromResult(SaveResult);
        }
    }
}
