using AxlProtocolMusic.WebApp.Components;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Extensions;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsecrets.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
ConfigureDevelopmentDataProtection(builder);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllersWithViews();
builder.Services.AddMongoDataAccess(builder.Configuration);
builder.Services.AddApplicationAuthentication(builder.Configuration);

var app = builder.Build();
var startupDiagnosticsEnabled = builder.Configuration.GetValue<bool>("StartupDiagnostics:ShowOnPage");
var detailedRequestErrorsEnabled = builder.Configuration.GetValue<bool>("StartupDiagnostics:DetailedRequestErrors");
string? startupError = null;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || detailedRequestErrorsEnabled)
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

try
{
    using var scope = app.Services.CreateScope();
    var adminIdentitySeeder = scope.ServiceProvider.GetRequiredService<AdminIdentitySeeder>();
    var aboutPageService = scope.ServiceProvider.GetRequiredService<IAboutPageService>();
    var newsArticleSeedService = scope.ServiceProvider.GetRequiredService<NewsArticleSeedService>();
    var releaseSeedService = scope.ServiceProvider.GetRequiredService<ReleaseSeedService>();
    var timelineEventService = scope.ServiceProvider.GetRequiredService<ITimelineEventService>();
    await adminIdentitySeeder.SeedAsync();
    await aboutPageService.SeedAsync();
    await newsArticleSeedService.SeedAsync();
    await releaseSeedService.SeedAsync();
    await timelineEventService.SeedAsync();
}
catch (Exception ex)
{
    startupError = ex.ToString();
    app.Logger.LogCritical(ex, "Application startup failed.");

    if (!startupDiagnosticsEnabled)
    {
        throw;
    }

    app.Map("/{**path}", async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "text/html; charset=utf-8";

        var encodedError = WebUtility.HtmlEncode(startupError);
        await context.Response.WriteAsync($$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <title>Startup Error</title>
                <style>
                    body { font-family: Consolas, "Courier New", monospace; margin: 2rem; background: #111827; color: #f9fafb; }
                    h1 { font-family: Segoe UI, Arial, sans-serif; }
                    pre { white-space: pre-wrap; word-break: break-word; background: #1f2937; padding: 1rem; border-radius: 8px; }
                </style>
            </head>
            <body>
                <h1>Application Startup Error</h1>
                <pre>{{encodedError}}</pre>
            </body>
            </html>
            """);
    });
}

app.Run();

void ConfigureDevelopmentDataProtection(WebApplicationBuilder webApplicationBuilder)
{
    if (!webApplicationBuilder.Environment.IsDevelopment())
    {
        return;
    }

    var keysDirectory = Path.Combine(webApplicationBuilder.Environment.ContentRootPath, ".localkeys");
    Directory.CreateDirectory(keysDirectory);

    webApplicationBuilder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));
}
