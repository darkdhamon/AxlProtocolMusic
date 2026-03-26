using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Extensions;
using AxlProtocolMusic.WebApp.Models.Identity;
using AxlProtocolMusic.WebApp.Repositories;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services;
using AxlProtocolMusic.WebApp.Services.Development;
using AxlProtocolMusic.WebApp.Services.Identity;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AxlProtocolMusic.WebApp.Tests.Extensions;

[TestFixture]
public sealed class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddMongoDataAccess_WithBlobConfiguration_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            [$"{MongoDbSettings.SectionName}:ConnectionString"] = "mongodb://localhost:27017",
            [$"{MongoDbSettings.SectionName}:DatabaseName"] = "AxlProtocolMusic",
            [$"{ImageStorageSettings.SectionName}:ConnectionString"] = "UseDevelopmentStorage=true",
            [$"{ImageStorageSettings.SectionName}:ContainerName"] = "images",
            [$"{ImageStorageSettings.SectionName}:UploadRoot"] = "uploads",
            [$"{ChatbotSettings.SectionName}:Enabled"] = "true",
            [$"{OpenAiChatSettings.SectionName}:ApiKey"] = "test-key",
            [$"{OpenAiChatSettings.SectionName}:Model"] = "gpt-5-mini"
        });

        var returnedServices = services.AddMongoDataAccess(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.That(returnedServices, Is.SameAs(services));
        AssertSingleton<IMongoDbService, MongoDbService>(services);
        AssertScopedOpenGeneric(services, typeof(IRepository<>), typeof(MongoRepository<>));
        AssertScoped<IAboutPageService, AboutPageService>(services);
        AssertScoped<IAnalyticsService, AnalyticsService>(services);
        AssertScoped<IChatbotBudgetService, ChatbotBudgetService>(services);
        AssertScoped<INewsArticleService, NewsArticleService>(services);
        AssertScoped<IPrivacyPreferencesService, PrivacyPreferencesService>(services);
        AssertScoped<IReleaseService, ReleaseService>(services);
        AssertScoped<ISiteChatbotContextBuilder, SiteChatbotContextBuilder>(services);
        AssertScoped<ITimelineEventService, TimelineEventService>(services);
        AssertScoped<MarkdownService, MarkdownService>(services);
        Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(IHttpContextAccessor)
            && descriptor.Lifetime == ServiceLifetime.Singleton));
        Assert.That(provider.GetRequiredService<ISiteChatbotService>(), Is.TypeOf<SiteChatbotService>());

        var imageDescriptor = FindDescriptor(services, typeof(IImageStorageService));
        Assert.That(imageDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        Assert.That(imageDescriptor.ImplementationType, Is.EqualTo(typeof(BlobImageStorageService)));

        var mongoOptions = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
        var imageOptions = provider.GetRequiredService<IOptions<ImageStorageSettings>>().Value;
        var chatbotOptions = provider.GetRequiredService<IOptions<ChatbotSettings>>().Value;
        var openAiOptions = provider.GetRequiredService<IOptions<OpenAiChatSettings>>().Value;

        Assert.That(mongoOptions.ConnectionString, Is.EqualTo("mongodb://localhost:27017"));
        Assert.That(mongoOptions.DatabaseName, Is.EqualTo("AxlProtocolMusic"));
        Assert.That(imageOptions.ConnectionString, Is.EqualTo("UseDevelopmentStorage=true"));
        Assert.That(imageOptions.ContainerName, Is.EqualTo("images"));
        Assert.That(chatbotOptions.Enabled, Is.True);
        Assert.That(openAiOptions.ApiKey, Is.EqualTo("test-key"));
        Assert.That(openAiOptions.Model, Is.EqualTo("gpt-5-mini"));
    }

    [Test]
    public void AddMongoDataAccess_WithoutBlobConfiguration_RegistersDiskImageStorage()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            [$"{MongoDbSettings.SectionName}:ConnectionString"] = "mongodb://localhost:27017",
            [$"{MongoDbSettings.SectionName}:DatabaseName"] = "AxlProtocolMusic",
            [$"{ImageStorageSettings.SectionName}:UploadRoot"] = "uploads"
        });

        services.AddMongoDataAccess(configuration);

        var imageDescriptor = FindDescriptor(services, typeof(IImageStorageService));
        Assert.That(imageDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
        Assert.That(imageDescriptor.ImplementationType, Is.EqualTo(typeof(DiskImageStorageService)));
    }

    [Test]
    public void AddApplicationAuthentication_WithMockIdentityProvider_RegistersIdentityAndConfiguresOptions()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            [$"{MongoDbSettings.SectionName}:ConnectionString"] = "mongodb://localhost:27017",
            [$"{MongoDbSettings.SectionName}:DatabaseName"] = "IdentityDb",
            [$"{AdminBootstrapSettings.SectionName}:UserName"] = "admin",
            [$"{AdminBootstrapSettings.SectionName}:Email"] = "admin@example.com",
            [$"{AdminBootstrapSettings.SectionName}:Password"] = "SecretPassword123!"
        });

        var returnedServices = services.AddApplicationAuthentication(configuration, AddMockIdentityProvider);
        using var provider = services.BuildServiceProvider();

        Assert.That(returnedServices, Is.SameAs(services));
        AssertScoped<IAdminIdentitySeeder, AdminIdentitySeeder>(services);
        AssertScoped<NewsArticleSeedService, NewsArticleSeedService>(services);
        AssertScoped<ReleaseSeedService, ReleaseSeedService>(services);
        AssertScoped<DevelopmentDatabaseResetService, DevelopmentDatabaseResetService>(services);
        Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(IAuthorizationService)));

        var identityOptions = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;
        Assert.That(identityOptions.Password.RequiredLength, Is.EqualTo(12));
        Assert.That(identityOptions.Password.RequireDigit, Is.True);
        Assert.That(identityOptions.Password.RequireLowercase, Is.True);
        Assert.That(identityOptions.Password.RequireUppercase, Is.True);
        Assert.That(identityOptions.Password.RequireNonAlphanumeric, Is.True);
        Assert.That(identityOptions.User.RequireUniqueEmail, Is.True);

        var adminOptions = provider.GetRequiredService<IOptions<AdminBootstrapSettings>>().Value;
        Assert.That(adminOptions.UserName, Is.EqualTo("admin"));
        Assert.That(adminOptions.Email, Is.EqualTo("admin@example.com"));
        Assert.That(adminOptions.Password, Is.EqualTo("SecretPassword123!"));

        var cookieOptions = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.That(cookieOptions.LoginPath.Value, Is.EqualTo("/login"));
        Assert.That(cookieOptions.AccessDeniedPath.Value, Is.EqualTo("/access-denied"));
        Assert.That(cookieOptions.ExpireTimeSpan, Is.EqualTo(TimeSpan.FromMinutes(30)));
        Assert.That(cookieOptions.SlidingExpiration, Is.True);
    }

    [Test]
    public void AddApplicationAuthentication_WhenConnectionStringIsMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            [$"{MongoDbSettings.SectionName}:ConnectionString"] = " ",
            [$"{MongoDbSettings.SectionName}:DatabaseName"] = "IdentityDb"
        });

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddApplicationAuthentication(configuration));

        Assert.That(exception!.Message, Is.EqualTo("MongoDb:ConnectionString must be configured."));
    }

    [Test]
    public void AddApplicationAuthentication_WhenDatabaseNameIsMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            [$"{MongoDbSettings.SectionName}:ConnectionString"] = "mongodb://localhost:27017",
            [$"{MongoDbSettings.SectionName}:DatabaseName"] = " "
        });

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddApplicationAuthentication(configuration));

        Assert.That(exception!.Message, Is.EqualTo("MongoDb:DatabaseName must be configured."));
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static IdentityBuilder AddMockIdentityProvider(
        IServiceCollection services,
        Action<IdentityOptions> configureIdentity,
        string connectionString)
    {
        Assert.That(connectionString, Does.StartWith("mongodb://localhost"));
        Assert.That(connectionString, Does.EndWith("/IdentityDb"));

        return services.AddIdentity<ApplicationUser, ApplicationRole>(configureIdentity);
    }

    private static ServiceDescriptor FindDescriptor(IServiceCollection services, Type serviceType)
    {
        return services.Last(descriptor => descriptor.ServiceType == serviceType);
    }

    private static void AssertSingleton<TService, TImplementation>(IServiceCollection services)
    {
        AssertDescriptor<TService, TImplementation>(services, ServiceLifetime.Singleton);
    }

    private static void AssertScoped<TService, TImplementation>(IServiceCollection services)
    {
        AssertDescriptor<TService, TImplementation>(services, ServiceLifetime.Scoped);
    }

    private static void AssertDescriptor<TService, TImplementation>(IServiceCollection services, ServiceLifetime lifetime)
    {
        var descriptor = FindDescriptor(services, typeof(TService));
        Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(TImplementation)));
        Assert.That(descriptor.Lifetime, Is.EqualTo(lifetime));
    }

    private static void AssertScopedOpenGeneric(IServiceCollection services, Type serviceType, Type implementationType)
    {
        var descriptor = FindDescriptor(services, serviceType);
        Assert.That(descriptor.ImplementationType, Is.EqualTo(implementationType));
        Assert.That(descriptor.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
    }
}
