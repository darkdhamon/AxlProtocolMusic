using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AxlProtocolMusic.WebApp.Services.Development;

public sealed class DevelopmentDatabaseResetService
{
    private readonly MongoDbSettings _mongoDbSettings;
    private readonly AdminIdentitySeeder _adminIdentitySeeder;
    private readonly IAboutPageService _aboutPageService;
    private readonly NewsArticleSeedService _newsArticleSeedService;
    private readonly ReleaseSeedService _releaseSeedService;
    private readonly ITimelineEventService _timelineEventService;

    public DevelopmentDatabaseResetService(
        IOptions<MongoDbSettings> mongoDbOptions,
        AdminIdentitySeeder adminIdentitySeeder,
        IAboutPageService aboutPageService,
        NewsArticleSeedService newsArticleSeedService,
        ReleaseSeedService releaseSeedService,
        ITimelineEventService timelineEventService)
    {
        _mongoDbSettings = mongoDbOptions.Value;
        _adminIdentitySeeder = adminIdentitySeeder;
        _aboutPageService = aboutPageService;
        _newsArticleSeedService = newsArticleSeedService;
        _releaseSeedService = releaseSeedService;
        _timelineEventService = timelineEventService;
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

        var client = new MongoClient(_mongoDbSettings.ConnectionString);
        await client.DropDatabaseAsync(_mongoDbSettings.DatabaseName);
        await _adminIdentitySeeder.SeedAsync();
        await _aboutPageService.SeedAsync();
        await _newsArticleSeedService.SeedAsync();
        await _releaseSeedService.SeedAsync();
        await _timelineEventService.SeedAsync();
    }
}
