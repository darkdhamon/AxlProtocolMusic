using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Development;
using AxlProtocolMusic.WebApp.Services.Identity;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class DevelopmentDatabaseResetServiceTests
{
    [Test]
    public async Task ResetAsync_WhenConnectionTargetsAzure_OnlyResetsBootstrapAdmin()
    {
        var adminIdentitySeeder = new Mock<IAdminIdentitySeeder>(MockBehavior.Strict);
        adminIdentitySeeder.Setup(instance => instance.ResetBootstrapAdminAsync()).Returns(Task.CompletedTask);

        var aboutPageService = new Mock<IAboutPageService>(MockBehavior.Strict);
        var newsRepository = new Mock<IRepository<NewsArticle>>(MockBehavior.Strict);
        var releaseRepository = new Mock<IRepository<Release>>(MockBehavior.Strict);
        var newsArticleSeedService = new NewsArticleSeedService(newsRepository.Object);
        var releaseSeedService = new ReleaseSeedService(releaseRepository.Object);
        var timelineEventService = new Mock<ITimelineEventService>(MockBehavior.Strict);
        var mongoClient = new Mock<IMongoClient>(MockBehavior.Strict);

        var service = new DevelopmentDatabaseResetService(
            Options.Create(new MongoDbSettings
            {
                ConnectionString = "mongodb://axl-dev.mongo.cosmos.azure.com:10255",
                DatabaseName = "AxlProtocolMusicDev"
            }),
            adminIdentitySeeder.Object,
            aboutPageService.Object,
            newsArticleSeedService,
            releaseSeedService,
            timelineEventService.Object,
            _ => mongoClient.Object);

        await service.ResetAsync();

        adminIdentitySeeder.Verify(instance => instance.ResetBootstrapAdminAsync(), Times.Once);
        mongoClient.Verify(instance => instance.DropDatabaseAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Test]
    public async Task ResetAsync_WhenConnectionIsNotAzure_DropsDatabaseAndReseedsEverything()
    {
        var adminIdentitySeeder = new Mock<IAdminIdentitySeeder>(MockBehavior.Strict);
        adminIdentitySeeder.Setup(instance => instance.ResetBootstrapAdminAsync()).Returns(Task.CompletedTask);

        var aboutPageService = new Mock<IAboutPageService>(MockBehavior.Strict);
        aboutPageService.Setup(instance => instance.SeedAsync(default)).Returns(Task.CompletedTask);

        var newsRepository = new Mock<IRepository<NewsArticle>>(MockBehavior.Strict);
        newsRepository.Setup(instance => instance.GetAllAsync(default)).ReturnsAsync([]);
        newsRepository.Setup(instance => instance.CreateAsync(It.IsAny<NewsArticle>(), default)).Returns(Task.CompletedTask);
        var newsArticleSeedService = new NewsArticleSeedService(newsRepository.Object);

        var releaseRepository = new Mock<IRepository<Release>>(MockBehavior.Strict);
        releaseRepository.Setup(instance => instance.GetAllAsync(default)).ReturnsAsync([]);
        releaseRepository.Setup(instance => instance.CreateAsync(It.IsAny<Release>(), default)).Returns(Task.CompletedTask);
        var releaseSeedService = new ReleaseSeedService(releaseRepository.Object);

        var timelineEventService = new Mock<ITimelineEventService>(MockBehavior.Strict);
        timelineEventService.Setup(instance => instance.SeedAsync(default)).Returns(Task.CompletedTask);

        var mongoClient = new Mock<IMongoClient>(MockBehavior.Strict);
        mongoClient
            .Setup(instance => instance.DropDatabaseAsync("AxlProtocolMusicDev", default))
            .Returns(Task.CompletedTask);

        var service = new DevelopmentDatabaseResetService(
            Options.Create(new MongoDbSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "AxlProtocolMusicDev"
            }),
            adminIdentitySeeder.Object,
            aboutPageService.Object,
            newsArticleSeedService,
            releaseSeedService,
            timelineEventService.Object,
            _ => mongoClient.Object);

        await service.ResetAsync();

        mongoClient.Verify(instance => instance.DropDatabaseAsync("AxlProtocolMusicDev", default), Times.Once);
        adminIdentitySeeder.Verify(instance => instance.ResetBootstrapAdminAsync(), Times.Once);
        aboutPageService.Verify(instance => instance.SeedAsync(default), Times.Once);
        newsRepository.Verify(instance => instance.GetAllAsync(default), Times.Once);
        newsRepository.Verify(instance => instance.CreateAsync(It.IsAny<NewsArticle>(), default), Times.AtLeastOnce);
        releaseRepository.Verify(instance => instance.GetAllAsync(default), Times.Once);
        releaseRepository.Verify(instance => instance.CreateAsync(It.IsAny<Release>(), default), Times.AtLeastOnce);
        timelineEventService.Verify(instance => instance.SeedAsync(default), Times.Once);
    }
}
