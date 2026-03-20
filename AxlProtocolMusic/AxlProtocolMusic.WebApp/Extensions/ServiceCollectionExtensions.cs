using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Repositories;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Interfaces;

namespace AxlProtocolMusic.WebApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection(MongoDbSettings.SectionName));

        services.AddSingleton<IMongoDbService, MongoDbService>();
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

        return services;
    }
}
