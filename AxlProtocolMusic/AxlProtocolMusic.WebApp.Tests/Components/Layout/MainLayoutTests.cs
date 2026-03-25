using AxlProtocolMusic.WebApp.Components.Layout;
using AxlProtocolMusic.WebApp.Components.Common;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AxlProtocolMusic.WebApp.Tests.Components.Layout;

[TestFixture]
public sealed class MainLayoutTests
{
    [Test]
    public void MainLayout_WhenThemeInitializesToDark_ShowsDarkModeAsOn()
    {
        using var context = CreateContext(isDevelopment: false);
        context.JSInterop.Setup<string>("axlTheme.initializeTheme").SetResult("dark");

        var cut = RenderMainLayout(context);

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find(".theme-switch-state").TextContent, Is.EqualTo("On"));
        });
    }

    [Test]
    public void MainLayout_WhenViewingReleaseDetails_ShowsBackToReleasesLink()
    {
        using var context = CreateContext(isDevelopment: false);
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/releases/signals");

        var cut = RenderMainLayout(context);

        Assert.That(cut.Markup, Does.Contain("Back to Releases"));
        Assert.That(cut.Markup, Does.Contain("href=\"/releases\""));
    }

    [Test]
    public void MainLayout_WhenAdminViewsNews_ShowsCreateArticleLink()
    {
        using var context = CreateContext(isDevelopment: false);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/news");

        var cut = RenderMainLayout(context);

        Assert.That(cut.Markup, Does.Contain("Create Article"));
        Assert.That(cut.Markup, Does.Contain("href=\"/news?editor=new\""));
    }

    [Test]
    public void MainLayout_WhenAdminViewsTimelineInDevelopment_ShowsDevelopmentActions()
    {
        using var context = CreateContext(isDevelopment: true);
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/timeline");

        var cut = RenderMainLayout(context);

        Assert.That(cut.Markup, Does.Contain("Add Timeline Event"));
        Assert.That(cut.Markup, Does.Contain("href=\"/timeline?editor=new\""));
        Assert.That(cut.Markup, Does.Contain("Reset Dev DB"));
        Assert.That(cut.Markup, Does.Contain("action=\"/account/reset-development-database\""));
    }

    [Test]
    public void MainLayout_WhenNotInDevelopment_HidesDevelopmentResetAction()
    {
        using var context = CreateContext(isDevelopment: false);

        var cut = RenderMainLayout(context);

        Assert.That(cut.Markup, Does.Not.Contain("Reset Dev DB"));
        Assert.That(cut.Markup, Does.Not.Contain("/account/reset-development-database"));
    }

    private static BunitContext CreateContext(bool isDevelopment)
    {
        var context = new BunitContext();
        context.AddAuthorization().SetNotAuthorized();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<string>("axlTheme.initializeTheme").SetResult("light");
        context.ComponentFactories.AddStub<NavMenu>();
        context.ComponentFactories.AddStub<SiteChatbot>();
        context.Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(isDevelopment));
        context.Services.AddSingleton<IChatbotBudgetService, FakeChatbotBudgetService>();
        context.Services.AddSingleton<ISiteChatbotService, FakeSiteChatbotService>();
        context.Services.AddSingleton<IOptions<ChatbotSettings>>(Options.Create(new ChatbotSettings
        {
            Enabled = true
        }));
        return context;
    }

    private static IRenderedComponent<MainLayout> RenderMainLayout(BunitContext context)
    {
        return context.Render<MainLayout>();
    }

    private sealed class FakeHostEnvironment(bool isDevelopment) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = isDevelopment ? Environments.Development : Environments.Production;

        public string ApplicationName { get; set; } = "AxlProtocolMusic.WebApp";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeChatbotBudgetService : IChatbotBudgetService
    {
        public Task<ChatbotBudgetSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatbotBudgetSummary());

        public Task<ChatbotBudgetSummary> RecordUsageAsync(string model, long inputTokens, long outputTokens, long cachedInputTokens, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatbotBudgetSummary());

        public Task DisableForQuotaErrorAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordFailureAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ChatbotBudgetSummary> SetManualDisabledAsync(bool isDisabled, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatbotBudgetSummary { IsManuallyDisabled = isDisabled });

        public Task ResetAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeSiteChatbotService : ISiteChatbotService
    {
        public Task<ChatbotMessageResponse> GenerateReplyAsync(string message, IReadOnlyList<ChatbotConversationMessage>? history = null, ChatbotPageContext? currentPage = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatbotMessageResponse());
    }
}
