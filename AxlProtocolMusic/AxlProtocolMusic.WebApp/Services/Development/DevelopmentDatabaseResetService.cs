using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AxlProtocolMusic.WebApp.Services.Development;

public sealed class DevelopmentDatabaseResetService
{
    private readonly Func<string, IMongoClient> _mongoClientFactory;
    private readonly MongoDbSettings _mongoDbSettings;
    private readonly IAdminIdentitySeeder _adminIdentitySeeder;
    private readonly IAboutPageService _aboutPageService;
    private readonly NewsArticleSeedService _newsArticleSeedService;
    private readonly ReleaseSeedService _releaseSeedService;
    private readonly ITimelineEventService _timelineEventService;

    public DevelopmentDatabaseResetService(
        IOptions<MongoDbSettings> mongoDbOptions,
        IAdminIdentitySeeder adminIdentitySeeder,
        IAboutPageService aboutPageService,
        NewsArticleSeedService newsArticleSeedService,
        ReleaseSeedService releaseSeedService,
        ITimelineEventService timelineEventService)
        : this(
            mongoDbOptions,
            adminIdentitySeeder,
            aboutPageService,
            newsArticleSeedService,
            releaseSeedService,
            timelineEventService,
            connectionString => new MongoClient(connectionString))
    {
    }

    public DevelopmentDatabaseResetService(
        IOptions<MongoDbSettings> mongoDbOptions,
        IAdminIdentitySeeder adminIdentitySeeder,
        IAboutPageService aboutPageService,
        NewsArticleSeedService newsArticleSeedService,
        ReleaseSeedService releaseSeedService,
        ITimelineEventService timelineEventService,
        Func<string, IMongoClient> mongoClientFactory)
    {
        _mongoDbSettings = mongoDbOptions.Value;
        _adminIdentitySeeder = adminIdentitySeeder;
        _aboutPageService = aboutPageService;
        _newsArticleSeedService = newsArticleSeedService;
        _releaseSeedService = releaseSeedService;
        _timelineEventService = timelineEventService;
        _mongoClientFactory = mongoClientFactory;
    }

    public async Task ResetAsync()
    {
        if (string.IsNullOrWhiteSpace(_mongoDbSettings.ConnectionString))
        {
            throw new InvalidOperationException("MongoDb:ConnectionString must be configured.");
        }

        if (string.IsNullOrWhiteSpace(_mongoDbSettings.DatabaseName))
        {
            throw new InvalidOperationException("MongoDb:DatabaseName must be configured.");
        }

        if (MongoDbConnectionStringClassifier.ContainsAzureHost(_mongoDbSettings.ConnectionString))
        {
            await _adminIdentitySeeder.ResetBootstrapAdminAsync();
            return;
        }

        var client = _mongoClientFactory(_mongoDbSettings.ConnectionString);
        await client.DropDatabaseAsync(_mongoDbSettings.DatabaseName);
        await _adminIdentitySeeder.ResetBootstrapAdminAsync();
        await _aboutPageService.SeedAsync();
        await _newsArticleSeedService.SeedAsync();
        await _releaseSeedService.SeedAsync();
        await _timelineEventService.SeedAsync();
    }
}
