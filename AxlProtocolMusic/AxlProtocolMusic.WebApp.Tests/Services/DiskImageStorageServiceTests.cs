using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class DiskImageStorageServiceTests
{
    private List<string> _directoriesToDelete = [];

    [TearDown]
    public void TearDown()
    {
        foreach (var directory in _directoriesToDelete)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    public void SaveReleaseImageAsync_WhenFileIsEmpty_Throws()
    {
        var service = CreateService(out _);
        using var stream = new MemoryStream();
        IFormFile file = new FormFile(stream, 0, 0, "image", "cover.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var act = async () => await service.SaveReleaseImageAsync(file);

        Assert.That(act, Throws.InvalidOperationException.With.Message.EqualTo("The uploaded image file is empty."));
    }

    [Test]
    public void SaveReleaseImageAsync_WhenFileIsTooLarge_Throws()
    {
        var service = CreateService(out _, maxFileSizeBytes: 4);
        using var stream = new MemoryStream(new byte[5]);
        IFormFile file = new FormFile(stream, 0, stream.Length, "image", "cover.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var act = async () => await service.SaveReleaseImageAsync(file);

        Assert.That(act, Throws.InvalidOperationException.With.Message.EqualTo("The uploaded image exceeds the size limit."));
    }

    [Test]
    public void SaveReleaseImageAsync_WhenContentTypeIsUnsupported_Throws()
    {
        var service = CreateService(out _);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        IFormFile file = new FormFile(stream, 0, stream.Length, "image", "cover.bmp")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/bmp"
        };

        var act = async () => await service.SaveReleaseImageAsync(file);

        Assert.That(act, Throws.InvalidOperationException.With.Message.EqualTo("Only JPG, PNG, WEBP, and GIF images are supported."));
    }

    [Test]
    public async Task SaveReleaseImageAsync_WhenFileHasNoExtension_UsesContentTypeExtensionAndWritesFile()
    {
        var service = CreateService(out var webRootPath);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        IFormFile file = new FormFile(stream, 0, stream.Length, "image", "cover")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var result = await service.SaveReleaseImageAsync(file);

        Assert.That(result.Url, Does.StartWith("/uploads/releases/"));
        Assert.That(result.Url, Does.EndWith(".png"));
        Assert.That(result.StoragePath, Does.StartWith("uploads/releases/"));
        Assert.That(result.StoragePath, Does.EndWith(".png"));

        var physicalPath = Path.Combine(webRootPath, result.StoragePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        Assert.That(File.Exists(physicalPath), Is.True);
        Assert.That(new FileInfo(physicalPath).Length, Is.EqualTo(4));
    }

    [Test]
    public async Task SaveReleaseImageAsync_WhenFileHasExtension_KeepsProvidedExtension()
    {
        var service = CreateService(out _);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        IFormFile file = new FormFile(stream, 0, stream.Length, "image", "cover.custom.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var result = await service.SaveReleaseImageAsync(file);

        Assert.That(result.Url, Does.EndWith(".png"));
        Assert.That(result.StoragePath, Does.EndWith(".png"));
    }

    [Test]
    public async Task DeleteAsync_WhenFileExists_DeletesIt()
    {
        var service = CreateService(out var webRootPath);
        var relativePath = Path.Combine("uploads", "releases", "delete-me.png");
        var physicalPath = Path.Combine(webRootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
        await File.WriteAllBytesAsync(physicalPath, [1, 2, 3]);

        await service.DeleteAsync("/uploads/releases/delete-me.png");

        Assert.That(File.Exists(physicalPath), Is.False);
    }

    [Test]
    public async Task DeleteAsync_WhenStoragePathIsBlank_DoesNothing()
    {
        var service = CreateService(out _);

        await service.DeleteAsync("");
        await service.DeleteAsync("   ");
        await service.DeleteAsync("/uploads/releases/missing.png");

        Assert.Pass();
    }

    private DiskImageStorageService CreateService(out string webRootPath, long maxFileSizeBytes = 1024)
    {
        webRootPath = Path.Combine(
            "C:\\GitHub\\AxlProtocolMusic\\_buildcheck",
            "test-webroot-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(webRootPath);
        _directoriesToDelete.Add(webRootPath);

        var environment = new FakeWebHostEnvironment
        {
            WebRootPath = webRootPath,
            ContentRootPath = webRootPath
        };

        return new DiskImageStorageService(
            environment,
            Options.Create(new ImageStorageSettings
            {
                UploadRoot = "uploads",
                MaxFileSizeBytes = maxFileSizeBytes
            }));
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "AxlProtocolMusic.WebApp.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = "Development";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
