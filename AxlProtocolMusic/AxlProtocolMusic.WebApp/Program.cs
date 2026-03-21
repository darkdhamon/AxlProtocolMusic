using AxlProtocolMusic.WebApp.Components;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Extensions;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.Development;
using AxlProtocolMusic.WebApp.Services.Identity;
using Microsoft.Extensions.Options;
using Mongo2Go;

var builder = WebApplication.CreateBuilder(args);

ConfigureDevelopmentMongoDb(builder);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllersWithViews();
builder.Services.AddMongoDataAccess(builder.Configuration);
builder.Services.AddApplicationAuthentication(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var adminIdentitySeeder = scope.ServiceProvider.GetRequiredService<AdminIdentitySeeder>();
    var aboutPageService = scope.ServiceProvider.GetRequiredService<IAboutPageService>();
    var releaseSeedService = scope.ServiceProvider.GetRequiredService<ReleaseSeedService>();
    var timelineEventService = scope.ServiceProvider.GetRequiredService<ITimelineEventService>();
    await adminIdentitySeeder.SeedAsync();
    await aboutPageService.SeedAsync();
    await releaseSeedService.SeedAsync();
    await timelineEventService.SeedAsync();
}

app.Run();

void ConfigureDevelopmentMongoDb(WebApplicationBuilder webApplicationBuilder)
{
    if (!webApplicationBuilder.Environment.IsDevelopment())
    {
        return;
    }

    var developmentMongoSettings = webApplicationBuilder.Configuration
        .GetSection(DevelopmentMongoDbSettings.SectionName)
        .Get<DevelopmentMongoDbSettings>()
        ?? new DevelopmentMongoDbSettings();

    if (!developmentMongoSettings.Enabled)
    {
        return;
    }

    var dataDirectory = Path.GetFullPath(
        Path.Combine(webApplicationBuilder.Environment.ContentRootPath, developmentMongoSettings.DataDirectory));

    Directory.CreateDirectory(dataDirectory);

    var runner = MongoDbRunner.StartForDebugging(dataDirectory: dataDirectory);

    webApplicationBuilder.Services.AddSingleton(new DevelopmentMongoDbRunner(runner));

    webApplicationBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        [$"{MongoDbSettings.SectionName}:ConnectionString"] = runner.ConnectionString,
        [$"{MongoDbSettings.SectionName}:DatabaseName"] = developmentMongoSettings.DatabaseName
    });
}
