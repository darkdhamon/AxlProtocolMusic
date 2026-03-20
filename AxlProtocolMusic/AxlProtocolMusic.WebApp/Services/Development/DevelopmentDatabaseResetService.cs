using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Services.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AxlProtocolMusic.WebApp.Services.Development;

public sealed class DevelopmentDatabaseResetService
{
    private readonly MongoDbSettings _mongoDbSettings;
    private readonly AdminIdentitySeeder _adminIdentitySeeder;

    public DevelopmentDatabaseResetService(
        IOptions<MongoDbSettings> mongoDbOptions,
        AdminIdentitySeeder adminIdentitySeeder)
    {
        _mongoDbSettings = mongoDbOptions.Value;
        _adminIdentitySeeder = adminIdentitySeeder;
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
    }
}
