using System.Linq.Expressions;
using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Models.Content;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class AdminDashboardPageTests
{
    [Test]
    public void AdminDashboard_RendersSnapshotAndAggregates()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("admin");
        context.Services.AddSingleton<IRepository<Release>>(new FakeReleaseRepository
        {
            Releases =
            [
                new Release { Id = "1", Title = "Signals", IsPublished = true },
                new Release { Id = "2", Title = "Vault", IsPublished = false }
            ]
        });
        context.Services.AddSingleton<IAboutPageService>(new FakeAboutPageService
        {
            Content = new AboutPageContent
            {
                HeroLead = "Lead copy"
            }
        });
        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService
        {
            Summary = new AnalyticsDashboardSummary
            {
                TotalVisits = 321,
                UniqueVisitors = 125,
                RepeatVisitors = 18,
                AverageDurationSeconds = 95,
                TopPages =
                [
                    new PageVisitAggregate
                    {
                        PagePath = "/releases/signals",
                        PageTitle = "Signals",
                        VisitCount = 20,
                        AverageDurationSeconds = 84
                    }
                ],
                TopRegions =
                [
                    new RegionVisitAggregate
                    {
                        Region = "Austin, TX",
                        VisitCount = 11
                    }
                ],
                TopExternalLinks =
                [
                    new ExternalLinkClickAggregate
                    {
                        DestinationUrl = "https://bandcamp.example/signals",
                        LinkLabel = "Bandcamp",
                        ClickCount = 9,
                        SourcePagePath = "/releases/signals"
                    }
                ]
            }
        });
        context.Services.AddSingleton<IChatbotBudgetService>(new FakeChatbotBudgetService
        {
            Summary = new ChatbotBudgetSummary
            {
                DisableThresholdUsd = 10m,
                TotalEstimatedCostUsd = 2.5m,
                TotalInputTokens = 12000,
                TotalOutputTokens = 4000,
                TotalCachedInputTokens = 1000,
                TotalRequestCount = 30,
                IsDisabled = false,
                IsManuallyDisabled = false,
                LastUpdatedUtc = new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero)
            }
        });
        context.Services.AddSingleton<IChatbotConversationLogService>(new FakeChatbotConversationLogService
        {
            Entries =
            [
                new ChatbotConversationLogEntry
                {
                    Id = "log-1",
                    CreatedAtUtc = new DateTimeOffset(2026, 4, 2, 18, 0, 0, TimeSpan.Zero),
                    Outcome = "completed",
                    UserMessage = "What are the latest releases?",
                    AssistantReply = "Use /releases to browse the newest catalog entries.",
                    PagePath = "/"
                }
            ]
        });
        context.Services.AddSingleton<IOptions<OpenAiChatSettings>>(Options.Create(new OpenAiChatSettings
        {
            ApiKey = "live-key"
        }));
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/admin?chatbotReset=true&chatbotOverrideChanged=true");

        var cut = context.Render<AdminDashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Dashboard"));
            Assert.That(cut.Markup, Does.Contain("published releases"));
            Assert.That(cut.Markup, Does.Contain("Chatbot budget counters were reset."));
            Assert.That(cut.Markup, Does.Contain("Chatbot manual override was updated."));
            Assert.That(cut.Markup, Does.Contain("Signals"));
            Assert.That(cut.Markup, Does.Contain("Austin, TX"));
            Assert.That(cut.Markup, Does.Contain("Bandcamp"));
            Assert.That(cut.Markup, Does.Contain("Ready"));
            Assert.That(cut.Markup, Does.Contain("Recent Anonymous Messages"));
            Assert.That(cut.Markup, Does.Contain("What are the latest releases?"));
            Assert.That(cut.Markup, Does.Contain("/admin/chatbot/messages.csv"));
        });
    }

    [Test]
    public void AdminDashboard_WhenApiKeyIsMissing_ShowsUnavailableReason()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("admin");
        context.Services.AddSingleton<IRepository<Release>>(new FakeReleaseRepository());
        context.Services.AddSingleton<IAboutPageService>(new FakeAboutPageService());
        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService());
        context.Services.AddSingleton<IChatbotBudgetService>(new FakeChatbotBudgetService
        {
            Summary = new ChatbotBudgetSummary()
        });
        context.Services.AddSingleton<IChatbotConversationLogService>(new FakeChatbotConversationLogService());
        context.Services.AddSingleton<IOptions<OpenAiChatSettings>>(Options.Create(new OpenAiChatSettings
        {
            ApiKey = string.Empty
        }));

        var cut = context.Render<AdminDashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Unavailable (missing API key)"));
            Assert.That(cut.Markup, Does.Contain("The chatbot is unavailable because the OpenAI API key has not been configured."));
        });
    }

    [Test]
    public void AdminDashboard_RecentMessagesPanel_UsesScrollableLayoutHooks()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("admin");
        context.Services.AddSingleton<IRepository<Release>>(new FakeReleaseRepository());
        context.Services.AddSingleton<IAboutPageService>(new FakeAboutPageService());
        context.Services.AddSingleton<IAnalyticsService>(new FakeAnalyticsService());
        context.Services.AddSingleton<IChatbotBudgetService>(new FakeChatbotBudgetService());
        context.Services.AddSingleton<IChatbotConversationLogService>(new FakeChatbotConversationLogService
        {
            Entries =
            [
                new ChatbotConversationLogEntry
                {
                    Id = "log-1",
                    CreatedAtUtc = new DateTimeOffset(2026, 4, 2, 18, 0, 0, TimeSpan.Zero),
                    Outcome = "completed",
                    UserMessage = "A very long user message that should live in the recent message panel.",
                    AssistantReply = "A very long assistant response that should be constrained within the scrolling message history panel instead of stretching the whole dashboard layout.",
                    PagePath = "/"
                }
            ]
        });
        context.Services.AddSingleton<IOptions<OpenAiChatSettings>>(Options.Create(new OpenAiChatSettings
        {
            ApiKey = "live-key"
        }));

        var cut = context.Render<AdminDashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("article.budget-panel"), Is.Not.Null);
            Assert.That(cut.Find("article.recent-messages-panel"), Is.Not.Null);
            Assert.That(cut.Find("article.pages-panel"), Is.Not.Null);
            Assert.That(cut.Find("article.regions-panel"), Is.Not.Null);
            Assert.That(cut.Find("article.links-panel"), Is.Not.Null);
            Assert.That(cut.Find("article.capabilities-panel"), Is.Not.Null);
            Assert.That(cut.Find("article.expansion-panel"), Is.Not.Null);
            Assert.That(cut.Find("ul.recent-message-list"), Is.Not.Null);
        });
    }

    private sealed class FakeReleaseRepository : IRepository<Release>
    {
        public IReadOnlyList<Release> Releases { get; set; } = [];

        public Task<IReadOnlyList<Release>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Releases);

        public Task<Release?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Releases.FirstOrDefault(release => release.Id == id));

        public Task<IReadOnlyList<Release>> FindAsync(Expression<Func<Release, bool>> filter, CancellationToken cancellationToken = default)
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
        public AboutPageContent Content { get; set; } = new();

        public Task<AboutPageContent> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Content);

        public Task UpdateAsync(AboutPageContent content, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SeedAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeAnalyticsService : IAnalyticsService
    {
        public AnalyticsDashboardSummary Summary { get; set; } = new();

        public Task RecordPageVisitAsync(Models.Analytics.PageVisitMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordExternalLinkClickAsync(Models.Analytics.ExternalLinkClickMetric metric, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteVisitorDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteVisitorLocationDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<AnalyticsDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);

        public Task<VisitorCollectedDataViewModel> GetVisitorCollectedDataAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(new VisitorCollectedDataViewModel());
    }

    private sealed class FakeChatbotBudgetService : IChatbotBudgetService
    {
        public ChatbotBudgetSummary Summary { get; set; } = new();

        public Task<ChatbotBudgetSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);

        public Task<ChatbotBudgetSummary> RecordUsageAsync(string model, long inputTokens, long outputTokens, long cachedInputTokens, CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);

        public Task DisableForQuotaErrorAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordFailureAsync(string model, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ResetAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ChatbotBudgetSummary> SetManualDisabledAsync(bool isDisabled, CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);
    }

    private sealed class FakeChatbotConversationLogService : IChatbotConversationLogService
    {
        public IReadOnlyList<ChatbotConversationLogEntry> Entries { get; set; } = [];

        public Task RecordAsync(string userMessage, string assistantReply, string outcome, ChatbotPageContext? currentPage = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ChatbotConversationLogEntry>> GetRecentAsync(int count = 25, CancellationToken cancellationToken = default)
            => Task.FromResult(Entries);

        public Task<IReadOnlyList<ChatbotConversationLogEntry>> GetExportAsync(int count = 5000, CancellationToken cancellationToken = default)
            => Task.FromResult(Entries);
    }
}
