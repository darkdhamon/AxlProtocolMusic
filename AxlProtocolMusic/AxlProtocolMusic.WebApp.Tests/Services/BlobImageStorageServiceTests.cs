using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class BlobImageStorageServiceTests
{
    [Test]
    public void Constructor_WhenConnectionStringIsMissing_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateService(new ImageStorageSettings
        {
            ConnectionString = " ",
            ContainerName = "media"
        }));

        Assert.That(exception!.Message, Is.EqualTo("ImageStorage:ConnectionString must be configured for blob storage."));
    }

    [Test]
    public void Constructor_WhenContainerNameIsMissing_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateService(new ImageStorageSettings
        {
            ConnectionString = ValidConnectionString,
            ContainerName = " "
        }));

        Assert.That(exception!.Message, Is.EqualTo("ImageStorage:ContainerName must be configured for blob storage."));
    }

    [Test]
    public void SaveReleaseImageAsync_WhenFileIsEmpty_ThrowsBeforeUpload()
    {
        var service = CreateService();
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
    public void SaveReleaseImageAsync_WhenFileIsTooLarge_ThrowsBeforeUpload()
    {
        var service = CreateService(maxFileSizeBytes: 4);
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
    public void SaveReleaseImageAsync_WhenContentTypeIsUnsupported_ThrowsBeforeUpload()
    {
        var service = CreateService();
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
    public void IsManagedImageUrl_ReturnsTrueOnlyForUrlsInTheConfiguredContainer()
    {
        var service = CreateService(new ImageStorageSettings
        {
            ConnectionString = ValidConnectionString,
            ContainerName = "media"
        });

        Assert.Multiple(() =>
        {
            Assert.That(service.IsManagedImageUrl("https://testaccount.blob.core.windows.net/media/releases/image.png"), Is.True);
            Assert.That(service.IsManagedImageUrl("https://testaccount.blob.core.windows.net/media/releases/folder%20name/image.png"), Is.True);
            Assert.That(service.IsManagedImageUrl("https://otheraccount.blob.core.windows.net/media/releases/image.png"), Is.False);
            Assert.That(service.IsManagedImageUrl("https://testaccount.blob.core.windows.net/other/releases/image.png"), Is.False);
            Assert.That(service.IsManagedImageUrl("/uploads/releases/image.png"), Is.False);
            Assert.That(service.IsManagedImageUrl(""), Is.False);
            Assert.That(service.IsManagedImageUrl(null), Is.False);
        });
    }

    [Test]
    public async Task DeleteAsync_WhenStoragePathIsBlank_DoesNothing()
    {
        var service = CreateService();

        await service.DeleteAsync("");
        await service.DeleteAsync("   ");

        Assert.Pass();
    }

    [Test]
    public void IsManagedImageUrl_WhenUrlIsNotAbsolute_ReturnsFalse()
    {
        var service = CreateService();

        Assert.That(service.IsManagedImageUrl("not-a-valid-url"), Is.False);
    }

    [Test]
    public void GetBlobName_WhenStoragePathIsManagedAbsoluteUrl_ReturnsRelativeBlobPath()
    {
        var service = CreateService();

        var blobName = InvokeGetBlobName(
            service,
            "https://testaccount.blob.core.windows.net/media/releases/folder%20name/image.png");

        Assert.That(blobName, Is.EqualTo("releases/folder name/image.png"));
    }

    [Test]
    public void GetBlobName_WhenStoragePathTargetsAnotherContainer_FallsBackToTrimmedPath()
    {
        var service = CreateService();

        var blobName = InvokeGetBlobName(
            service,
            "https://testaccount.blob.core.windows.net/other/releases/image.png");

        Assert.That(blobName, Is.EqualTo("https://testaccount.blob.core.windows.net/other/releases/image.png"));
    }

    [Test]
    public void GetBlobName_WhenStoragePathIsRelative_TrimsLeadingSlash()
    {
        var service = CreateService();

        var blobName = InvokeGetBlobName(service, "/releases/image.png");

        Assert.That(blobName, Is.EqualTo("releases/image.png"));
    }

    private static string InvokeGetBlobName(BlobImageStorageService service, string storagePath)
    {
        var method = typeof(BlobImageStorageService).GetMethod("GetBlobName", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        return (string)method!.Invoke(service, [storagePath])!;
    }

    private static BlobImageStorageService CreateService(ImageStorageSettings? settings = null, long maxFileSizeBytes = 1024)
    {
        return new BlobImageStorageService(
            Options.Create(settings ?? new ImageStorageSettings
            {
                ConnectionString = ValidConnectionString,
                ContainerName = "media",
                MaxFileSizeBytes = maxFileSizeBytes
            }));
    }

    private const string ValidConnectionString =
        "DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=;EndpointSuffix=core.windows.net";
}
