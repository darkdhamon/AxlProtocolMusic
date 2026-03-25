using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Extensions;
using AxlProtocolMusic.WebApp.Services.Identity;
using AxlProtocolMusic.WebApp.Services.Development;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mongo2Go;

namespace AxlProtocolMusic.WebApp.IntegrationTests.Extensions;

[TestFixture]
public sealed class ServiceCollectionExtensionsIntegrationTests
{
    private MongoDbRunner? runner;

    [TearDown]
    public void TearDown()
    {
        runner?.Dispose();
        runner = null;
    }

    [Test]
    public void AddApplicationAuthentication_WithMongoProvider_RegistersIdentityAndConfiguresOptions()
    {
        runner = MongoDbRunner.Start(singleNodeReplSet: true);

        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            [$"{MongoDbSettings.SectionName}:ConnectionString"] = runner.ConnectionString,
            [$"{MongoDbSettings.SectionName}:DatabaseName"] = $"IdentityDb_{Guid.NewGuid():N}",
            [$"{AdminBootstrapSettings.SectionName}:UserName"] = "admin",
            [$"{AdminBootstrapSettings.SectionName}:Email"] = "admin@example.com",
            [$"{AdminBootstrapSettings.SectionName}:Password"] = "SecretPassword123!"
        });

        var returnedServices = services.AddApplicationAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.That(returnedServices, Is.SameAs(services));
        Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(AdminIdentitySeeder)
            && descriptor.Lifetime == ServiceLifetime.Scoped));
        Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(NewsArticleSeedService)
            && descriptor.Lifetime == ServiceLifetime.Scoped));
        Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(ReleaseSeedService)
            && descriptor.Lifetime == ServiceLifetime.Scoped));
        Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(DevelopmentDatabaseResetService)
            && descriptor.Lifetime == ServiceLifetime.Scoped));
        Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(IAuthorizationService)));

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

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
