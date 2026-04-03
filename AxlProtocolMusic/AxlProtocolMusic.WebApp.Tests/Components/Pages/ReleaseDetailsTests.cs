using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class ReleaseDetailsTests
{
    [Test]
    public void ReleaseDetails_WhenReleaseIsMissing_RendersNotFoundState()
    {
        using var context = CreateContext(out var releaseService);
        releaseService.Release = null;

        var cut = context.Render<ReleaseDetails>(parameters => parameters
            .Add(component => component.Slug, "missing-release"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Release Not Found"));
            Assert.That(cut.Markup, Does.Contain("The release you requested could not be found."));
            Assert.That(cut.Markup, Does.Contain("href=\"/releases\""));
        });

        Assert.That(releaseService.RequestedSlug, Is.EqualTo("missing-release"));
        Assert.That(releaseService.LastIncludeUnpublished, Is.False);
    }

    [Test]
    public void ReleaseDetails_WhenReleaseExists_RendersPublicDetails()
    {
        using var context = CreateContext(out var releaseService);
        releaseService.Release = new ReleaseDetailsViewModel
        {
            Id = "release-1",
            Title = "Signals",
            Slug = "signals",
            ShortDescription = "A cinematic synth release.",
            CoverImageUrl = "https://cdn.example/signals.jpg",
            Story = "**Made at midnight.**",
            ReleaseType = "EP",
            Tags = ["Synthwave", "Instrumental"],
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(30),
            Credits =
            [
                new ReleaseCredit { Name = "Axl Protocol", Roles = ["Production", "Mix"] }
            ],
            Tracks =
            [
                new ReleaseTrack { Title = "Neon Run", Duration = "3:45", Lyrics = "Night drive" },
                new ReleaseTrack { Title = "Static Bloom", Duration = "4:15" }
            ],
            Links =
            [
                new ReleaseLink { PlatformName = "Bandcamp", Url = "https://bandcamp.example/signals" }
            ]
        };

        var cut = context.Render<ReleaseDetails>(parameters => parameters
            .Add(component => component.Slug, "signals"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("h1").TextContent, Is.EqualTo("Signals"));
            Assert.That(cut.Markup, Does.Contain("A cinematic synth release."));
            Assert.That(cut.Markup, Does.Contain("Upcoming Release"));
            Assert.That(cut.Markup, Does.Contain("Synthwave"));
            Assert.That(cut.Markup, Does.Contain("Instrumental"));
            Assert.That(cut.Markup, Does.Contain("Bandcamp"));
            Assert.That(cut.Markup, Does.Contain("Made at midnight."));
            Assert.That(cut.Markup, Does.Contain("Total Play Time:</strong> 8:00"));
            Assert.That(cut.Markup, Does.Contain("Axl Protocol"));
            Assert.That(cut.Markup, Does.Contain("Production, Mix"));
            Assert.That(cut.Markup, Does.Contain("Neon Run"));
            Assert.That(cut.Markup, Does.Contain("Static Bloom"));
        });

        var image = cut.Find("img");
        Assert.That(image.GetAttribute("src"), Is.EqualTo("https://cdn.example/signals.jpg"));
        Assert.That(releaseService.LastIncludeUnpublished, Is.False);
    }

    [Test]
    public void ReleaseDetails_WhenAdminIsViewing_RequestsUnpublishedRelease()
    {
        using var context = CreateContext(out var releaseService);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        releaseService.Release = new ReleaseDetailsViewModel
        {
            Id = "release-2",
            Title = "Vault",
            Slug = "vault",
            ShortDescription = "Private preview.",
            ReleaseType = "Single",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var cut = context.Render<ReleaseDetails>(parameters => parameters
            .Add(component => component.Slug, "vault"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Vault"));
        });

        Assert.That(releaseService.LastIncludeUnpublished, Is.True);
    }

    [Test]
    public void ReleaseDetails_WhenTrackHasLyrics_OpensAndClosesLyricsModal()
    {
        using var context = CreateContext(out var releaseService);
        releaseService.Release = new ReleaseDetailsViewModel
        {
            Id = "release-3",
            Title = "Signals",
            Slug = "signals",
            ShortDescription = "A cinematic synth release.",
            ReleaseType = "EP",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Tracks =
            [
                new ReleaseTrack
                {
                    Title = "Neon Run",
                    Lyrics = "Night drive"
                }
            ]
        };

        var cut = context.Render<ReleaseDetails>(parameters => parameters
            .Add(component => component.Slug, "signals"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("See Lyrics"));
        });

        cut.Find("button.btn.btn-outline-primary.btn-sm").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Neon Run Lyrics"));
            Assert.That(cut.Markup, Does.Contain("Night drive"));
        });

        cut.Find("button.btn-close").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Not.Contain("Track Lyrics"));
        });
    }

    [Test]
    public void ReleaseDetails_WhenDeleteFails_ShowsErrorAndDoesNotNavigate()
    {
        using var context = CreateContext(out var releaseService, out var imageStorageService);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        releaseService.Release = new ReleaseDetailsViewModel
        {
            Id = "release-4",
            Title = "Vault",
            Slug = "vault",
            ShortDescription = "Private preview.",
            ReleaseType = "Single",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-5),
            CoverImageUrl = "managed://vault-art"
        };
        releaseService.DeleteResult = new ReleaseDeleteResult
        {
            Succeeded = false,
            ErrorMessage = "Delete failed."
        };
        var navigation = context.Services.GetRequiredService<NavigationManager>();

        var cut = context.Render<ReleaseDetails>(parameters => parameters
            .Add(component => component.Slug, "vault"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Delete Release"));
        });

        cut.Find("button.btn.btn-outline-light.btn-sm").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Are you sure you want to delete"));
        });

        cut.Find("button.btn.btn-danger").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Delete failed."));
        });

        Assert.That(releaseService.LastDeletedSlug, Is.EqualTo("vault"));
        Assert.That(imageStorageService.DeletedStoragePaths, Is.Empty);
        Assert.That(navigation.Uri, Does.EndWith("/"));
    }

    [Test]
    public void ReleaseDetails_WhenDeleteSucceeds_DeletesManagedImageAndNavigatesToReleases()
    {
        using var context = CreateContext(out var releaseService, out var imageStorageService);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        releaseService.Release = new ReleaseDetailsViewModel
        {
            Id = "release-5",
            Title = "Vault",
            Slug = "vault",
            ShortDescription = "Private preview.",
            ReleaseType = "Single",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-5),
            CoverImageUrl = "managed://vault-art"
        };
        releaseService.ManagedImageUrls.Add("managed://vault-art");
        releaseService.DeleteResult = new ReleaseDeleteResult
        {
            Succeeded = true,
            ImageStoragePath = "managed://vault-art"
        };
        var navigation = context.Services.GetRequiredService<NavigationManager>();

        var cut = context.Render<ReleaseDetails>(parameters => parameters
            .Add(component => component.Slug, "vault"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Delete Release"));
        });

        cut.Find("button.btn.btn-outline-light.btn-sm").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Are you sure you want to delete"));
        });

        cut.Find("button.btn.btn-danger").Click();

        Assert.That(releaseService.LastDeletedSlug, Is.EqualTo("vault"));
        Assert.That(imageStorageService.DeletedStoragePaths, Is.EqualTo(["managed://vault-art"]));
        Assert.That(navigation.Uri, Does.Contain("/releases?success=Release%20deleted."));
    }

    [Test]
    public void ReleaseDetails_WhenShortDescriptionExceedsLimit_ShowsCounterAndPausesAutosave()
    {
        using var context = CreateContext(out var releaseService);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        releaseService.UpdateResult = new ReleaseUpdateResult
        {
            Succeeded = true,
            Slug = "signals"
        };
        releaseService.Release = new ReleaseDetailsViewModel
        {
            Id = "release-6",
            Title = "Signals",
            Slug = "signals",
            ShortDescription = "Short copy.",
            ReleaseType = "Single",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var cut = context.Render<ReleaseDetails>(parameters => parameters
            .Add(component => component.Slug, "signals"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("11/350 characters"));
        });

        var updatedDescription = new string('x', 351);
        cut.Find("#shortDescription").Input(updatedDescription);

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("351/350 characters"));
            Assert.That(cut.Markup, Does.Contain("Autosave is paused until the short description is 350 characters or fewer."));
            Assert.That(cut.Markup, Does.Contain("Autosave paused. Short description is 351/350 characters."));
        }, timeout: TimeSpan.FromSeconds(3));

        Thread.Sleep(850);
        Assert.That(releaseService.UpdateCallCount, Is.EqualTo(0));
    }

    [Test]
    public void ReleaseDetails_WhenExistingLongShortDescriptionIsUnchanged_AutosaveKeepsItWhileSavingOtherFields()
    {
        using var context = CreateContext(out var releaseService);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        var existingLongDescription = new string('l', 360);
        releaseService.UpdateResult = new ReleaseUpdateResult
        {
            Succeeded = true,
            Slug = "vault"
        };
        releaseService.Release = new ReleaseDetailsViewModel
        {
            Id = "release-7",
            Title = "Vault",
            Slug = "vault",
            ShortDescription = existingLongDescription,
            ReleaseType = "Single",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var cut = context.Render<ReleaseDetails>(parameters => parameters
            .Add(component => component.Slug, "vault"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("360/350 characters"));
        });

        cut.Find("#title").Input("Vault Deluxe");
        Thread.Sleep(1000);

        Assert.That(releaseService.UpdateCallCount, Is.EqualTo(1));
        Assert.That(releaseService.LastUpdateRequest, Is.Not.Null);
        Assert.That(releaseService.LastUpdateRequest!.Title, Is.EqualTo("Vault Deluxe"));
        Assert.That(releaseService.LastUpdateRequest.ShortDescription, Is.EqualTo(existingLongDescription));
    }

    private static BunitContext CreateContext(out FakeReleaseService releaseService)
    {
        return CreateContext(out releaseService, out _);
    }

    private static BunitContext CreateContext(out FakeReleaseService releaseService, out FakeImageStorageService imageStorageService)
    {
        var context = new BunitContext();
        releaseService = new FakeReleaseService();
        imageStorageService = new FakeImageStorageService();
        context.AddAuthorization().SetNotAuthorized();
        context.Services.AddSingleton<IReleaseService>(releaseService);
        context.Services.AddSingleton<IImageStorageService>(imageStorageService);
        context.Services.AddSingleton<MarkdownService>();
        return context;
    }

    private sealed class FakeReleaseService : IReleaseService
    {
        public ReleaseDetailsViewModel? Release { get; set; }
        public ReleaseUpdateResult UpdateResult { get; set; } = new();
        public ReleaseUpdateRequest? LastUpdateRequest { get; private set; }
        public int UpdateCallCount { get; private set; }

        public string? RequestedSlug { get; private set; }

        public bool LastIncludeUnpublished { get; private set; }

        public string? LastDeletedSlug { get; private set; }

        public ReleaseDeleteResult DeleteResult { get; set; } = new();

        public HashSet<string> ManagedImageUrls { get; } = [];

        public Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FeaturedReleaseViewModel>>([]);

        public Task<PagedReleaseResult> GetPagedReleasesAsync(string? searchTerm, int pageNumber, int pageSize, bool includeUnpublished = false, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedReleaseResult());

        public Task<ReleaseDetailsViewModel?> GetReleaseBySlugAsync(string slug, bool includeUnpublished = false, CancellationToken cancellationToken = default)
        {
            RequestedSlug = slug;
            LastIncludeUnpublished = includeUnpublished;
            return Task.FromResult(Release);
        }

        public Task<ReleaseUpdateResult> UpdateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
        {
            UpdateCallCount++;
            LastUpdateRequest = CloneRequest(request);
            return Task.FromResult(UpdateResult);
        }

        public Task<ReleaseCreateResult> CreateReleaseAsync(ReleaseUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseCreateResult());

        public Task<ReleaseDeleteResult> DeleteReleaseAsync(string slug, CancellationToken cancellationToken = default)
        {
            LastDeletedSlug = slug;
            return Task.FromResult(DeleteResult);
        }

        public Task<string> GenerateUniqueSlugAsync(string? value, CancellationToken cancellationToken = default)
            => Task.FromResult(value ?? string.Empty);

        public Task<IReadOnlyList<string>> GetKnownCreditRolesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Production", "Mix"]);

        public Task<IReadOnlyList<string>> GetKnownContributorNamesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Axl Protocol"]);

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetKnownContributorRolesByNameAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(new Dictionary<string, IReadOnlyList<string>>
            {
                ["Axl Protocol"] = ["Mix", "Production"]
            });

        public Task<IReadOnlyList<string>> GetKnownTagsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Synthwave", "Instrumental"]);

        public bool IsManagedImageUrl(string? imageUrl) => !string.IsNullOrWhiteSpace(imageUrl) && ManagedImageUrls.Contains(imageUrl);

        private static ReleaseUpdateRequest CloneRequest(ReleaseUpdateRequest request)
        {
            return new ReleaseUpdateRequest
            {
                OriginalSlug = request.OriginalSlug,
                Title = request.Title,
                Slug = request.Slug,
                ShortDescription = request.ShortDescription,
                CoverImageUrl = request.CoverImageUrl,
                Story = request.Story,
                Lyrics = request.Lyrics,
                ReleaseDate = request.ReleaseDate,
                IsPublished = request.IsPublished,
                ReleaseTypeOverride = request.ReleaseTypeOverride,
                Credits = request.Credits
                    .Select(credit => new ReleaseCredit
                    {
                        Name = credit.Name,
                        Roles = credit.Roles.ToList()
                    })
                    .ToList(),
                Tracks = request.Tracks
                    .Select(track => new ReleaseTrack
                    {
                        Title = track.Title,
                        Duration = track.Duration,
                        Lyrics = track.Lyrics
                    })
                    .ToList(),
                Links = request.Links
                    .Select(link => new ReleaseLink
                    {
                        PlatformName = link.PlatformName,
                        Url = link.Url
                    })
                    .ToList(),
                Tags = request.Tags.ToList()
            };
        }
    }

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public List<string> DeletedStoragePaths { get; } = [];

        public Task<ImageSaveResult> SaveReleaseImageAsync(IFormFile file, CancellationToken cancellationToken = default)
            => Task.FromResult(new ImageSaveResult());

        public bool IsManagedImageUrl(string? imageUrl) => false;

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            DeletedStoragePaths.Add(storagePath);
            return Task.CompletedTask;
        }
    }
}
