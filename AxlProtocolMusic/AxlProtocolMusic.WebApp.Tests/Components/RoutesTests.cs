using AxlProtocolMusic.WebApp.Components;
using AxlProtocolMusic.WebApp.Components.Common;
using AxlProtocolMusic.WebApp.Components.Layout;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AxlProtocolMusic.WebApp.Tests.Components;

[TestFixture]
public sealed class RoutesTests
{
    [Test]
    public void Routes_WhenUserIsAnonymous_RoutesToLoginOnProtectedPage()
    {
        using var context = CreateContextWithMainLayout();
        context.AddAuthorization().SetNotAuthorized();
        context.Services.AddSingleton<INewsArticleService>(new FakeNewsArticleService());
        context.Services.AddSingleton<IReleaseService>(new FakeReleaseService());
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("https://localhost/admin");

        var cut = context.Render<Routes>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(navigation.Uri, Is.EqualTo("https://localhost/login?returnUrl=%2Fadmin"));
        });
    }

    [Test]
    public void Routes_WhenUserLacksAdminRole_GoesToAccessDenied()
    {
        using var context = CreateContextWithMainLayout();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("viewer");
        context.Services.AddSingleton<INewsArticleService>(new FakeNewsArticleService());
        context.Services.AddSingleton<IReleaseService>(new FakeReleaseService());
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("https://localhost/admin");

        var cut = context.Render<Routes>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(navigation.Uri, Does.EndWith("/access-denied"));
        });
    }

    [Test]
    public void Routes_WhenRouteIsMissing_ShowsNotFoundPage()
    {
        using var context = CreateContextWithMainLayout();
        context.AddAuthorization().SetNotAuthorized();
        context.Services.AddSingleton<INewsArticleService>(new FakeNewsArticleService
        {
            Articles =
            [
                new NewsArticle
                {
                    Id = "news-1",
                    Title = "Test article",
                    Slug = "test-article",
                    PublicationDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
                    IsPublished = true
                }
            ]
        });
        context.Services.AddSingleton<IReleaseService>(new FakeReleaseService
        {
            SearchResults =
            [
                new ReleaseListItemViewModel
                {
                    Title = "Test release",
                    Slug = "test-release",
                    ShortDescription = "A test release",
                    IsPublished = true
                }
            ]
        });
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("https://localhost/this-route-does-not-exist");

        var cut = context.Render<Routes>();

        Assert.That(cut.Markup, Does.Contain("404 Error"));
        Assert.That(cut.Markup, Does.Contain("Page not found"));
        Assert.That(cut.Markup, Does.Contain("The page you requested does not exist or may have moved."));
    }

    [Test]
    public void Routes_WhenAuthorizedAdmin_EntersAdminDashboard()
    {
        using var context = CreateContextWithMainLayout();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Admin");
        context.Services.AddSingleton<IRepository<Release>>(new FakeReleaseRepository
        {
            Releases =
            [
                new Release { Id = "release-1", Title = "Signal", Slug = "signal", IsPublished = true }
            ]
        });
        context.Services.AddSingleton<IAboutPageService>(new FakeAboutPageService());
        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService());
        context.Services.AddSingleton<IChatbotBudgetService>(new FakeChatbotBudgetService
        {
            Summary = new ChatbotBudgetSummary
            {
                DisableThresholdUsd = 10m,
                TotalEstimatedCostUsd = 0m
            }
        });
        context.Services.AddSingleton<IChatbotConversationLogService>(new FakeChatbotConversationLogService());
        context.Services.AddSingleton<IOptions<OpenAiChatSettings>>(Options.Create(new OpenAiChatSettings
        {
            ApiKey = "key"
        }));
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("https://localhost/admin");

        var cut = context.Render<Routes>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(navigation.Uri, Does.EndWith("/admin"));
            Assert.That(cut.Markup, Does.Contain("Admin"));
            Assert.That(cut.Markup, Does.Contain("Dashboard"));
            Assert.That(cut.Markup, Does.Contain("published releases"));
        });
    }

    private static BunitContext CreateContextWithMainLayout()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<string>("axlTheme.initializeTheme").SetResult("light");
        context.ComponentFactories.AddStub<NavMenu>();
        context.ComponentFactories.AddStub<SiteChatbot>();
        context.Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());
        return context;
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "AxlProtocolMusic.WebApp";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeNewsArticleService : INewsArticleService
    {
        public IReadOnlyList<NewsArticle> Articles { get; set; } = [];

        public Task<IReadOnlyList<NewsArticle>> GetArticlesAsync(
            bool includeUnpublished = false,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Articles);

        public Task<NewsArticle> CreateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new NewsArticle());

        public Task<NewsArticle> UpdateAsync(NewsArticleUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new NewsArticle());

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public bool IsManagedImageUrl(string? imageUrl) => false;
    }

    private sealed class FakeReleaseService : IReleaseService
    {
        public IReadOnlyList<FeaturedReleaseViewModel> FeaturedReleases { get; set; } = [];

        public IReadOnlyList<ReleaseListItemViewModel> SearchResults { get; set; } = [];

        public Task<IReadOnlyList<FeaturedReleaseViewModel>> GetFeaturedReleasesAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(FeaturedReleases);

        public Task<PagedReleaseResult> GetPagedReleasesAsync(
            string? searchTerm,
            int pageNumber,
            int pageSize,
            bool includeUnpublished = false,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedReleaseResult
            {
                Items = SearchResults
            });

        public Task<ReleaseDetailsViewModel?> GetReleaseBySlugAsync(
            string slug,
            bool includeUnpublished = false,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ReleaseDetailsViewModel?>(null);

        public Task<ReleaseUpdateResult> UpdateReleaseAsync(
            ReleaseUpdateRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseUpdateResult());

        public Task<ReleaseCreateResult> CreateReleaseAsync(
            ReleaseUpdateRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseCreateResult());

        public Task<ReleaseDeleteResult> DeleteReleaseAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(new ReleaseDeleteResult());

        public Task<string> GenerateUniqueSlugAsync(string? value, CancellationToken cancellationToken = default)
            => Task.FromResult(value ?? string.Empty);

        public Task<IReadOnlyList<string>> GetKnownCreditRolesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<string>> GetKnownContributorNamesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetKnownContributorRolesByNameAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(new Dictionary<string, IReadOnlyList<string>>());

        public Task<IReadOnlyList<string>> GetKnownTagsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public bool IsManagedImageUrl(string? imageUrl) => false;
    }

    private sealed class FakeReleaseRepository : IRepository<Release>
    {
        public IReadOnlyList<Release> Releases { get; set; } = [];

        public Task<IReadOnlyList<Release>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Releases);

        public Task<Release?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Releases.FirstOrDefault(release => release.Id == id));

        public Task<IReadOnlyList<Release>> FindAsync(System.Linq.Expressions.Expression<Func<Release, bool>> filter, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Release>>([]);

        public Task CreateAsync(Release document, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(Release document, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeAboutPageService : IAboutPageService
    {
        public Task<AboutPageContent> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AboutPageContent());

        public Task UpdateAsync(AboutPageContent content, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SeedAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeAnalyticsService : IAnalyticsService
    {
        public Task RecordPageVisitAsync(Models.Analytics.PageVisitMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordExternalLinkClickAsync(Models.Analytics.ExternalLinkClickMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteVisitorDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteVisitorLocationDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<AnalyticsDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AnalyticsDashboardSummary());

        public Task<VisitorCollectedDataViewModel> GetVisitorCollectedDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(new VisitorCollectedDataViewModel());
    }

    private sealed class FakeChatbotBudgetService : IChatbotBudgetService
    {
        public ChatbotBudgetSummary Summary { get; set; } = new();

        public Task<ChatbotBudgetSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);

        public Task<ChatbotBudgetSummary> RecordUsageAsync(
            string model,
            long inputTokens,
            long outputTokens,
            long cachedInputTokens,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);

        public Task DisableForQuotaErrorAsync(
            string model,
            string errorCode,
            string errorMessage,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordFailureAsync(
            string model,
            string errorCode,
            string errorMessage,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ResetAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ChatbotBudgetSummary> SetManualDisabledAsync(
            bool isDisabled,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);
    }

    private sealed class FakeChatbotConversationLogService : IChatbotConversationLogService
    {
        public Task RecordAsync(
            string userMessage,
            string assistantReply,
            string outcome,
            ChatbotPageContext? currentPage = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ChatbotConversationLogEntry>> GetRecentAsync(
            int count = 25,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ChatbotConversationLogEntry>>([]);

        public Task<IReadOnlyList<ChatbotConversationLogEntry>> GetExportAsync(
            int count = 5000,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ChatbotConversationLogEntry>>([]);
    }
}
