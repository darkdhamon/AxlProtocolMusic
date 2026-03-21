using AspNetCore.Identity.Mongo;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models.Identity;
using AxlProtocolMusic.WebApp.Repositories;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Development;
using AxlProtocolMusic.WebApp.Services.Identity;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using System;

namespace AxlProtocolMusic.WebApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection(MongoDbSettings.SectionName));
        services.Configure<ImageStorageSettings>(
            configuration.GetSection(ImageStorageSettings.SectionName));

        services.AddHttpContextAccessor();
        services.AddSingleton<IMongoDbService, MongoDbService>();
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));
        services.AddScoped<IAboutPageService, AboutPageService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IPrivacyPreferencesService, PrivacyPreferencesService>();
        services.AddScoped<IReleaseService, ReleaseService>();
        services.AddScoped<ITimelineEventService, TimelineEventService>();
        services.AddScoped<IImageStorageService, DiskImageStorageService>();
        services.AddScoped<MarkdownService>();

        return services;
    }

    public static IServiceCollection AddApplicationAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mongoSettings = configuration
            .GetSection(MongoDbSettings.SectionName)
            .Get<MongoDbSettings>()
            ?? new MongoDbSettings();

        services.Configure<AdminBootstrapSettings>(
            configuration.GetSection(AdminBootstrapSettings.SectionName));

        services
            .AddIdentityMongoDbProvider<ApplicationUser, ApplicationRole, string>(
                identity =>
                {
                    identity.Password.RequiredLength = 12;
                    identity.Password.RequireDigit = true;
                    identity.Password.RequireLowercase = true;
                    identity.Password.RequireUppercase = true;
                    identity.Password.RequireNonAlphanumeric = true;
                    identity.User.RequireUniqueEmail = true;
                },
                mongo =>
                {
                    mongo.ConnectionString = BuildIdentityConnectionString(mongoSettings);
                })
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/access-denied";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;
        });

        services.AddScoped<AdminIdentitySeeder>();
        services.AddScoped<ReleaseSeedService>();
        services.AddScoped<DevelopmentDatabaseResetService>();
        services.AddCascadingAuthenticationState();
        services.AddAuthorization();

        return services;
    }

    private static string BuildIdentityConnectionString(MongoDbSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException("MongoDb:ConnectionString must be configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.DatabaseName))
        {
            throw new InvalidOperationException("MongoDb:DatabaseName must be configured.");
        }

        var mongoUrlBuilder = new MongoUrlBuilder(settings.ConnectionString)
        {
            DatabaseName = settings.DatabaseName
        };

        return mongoUrlBuilder.ToString();
    }
}
