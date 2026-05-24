using System.Text.Json;
using AxlProtocolMusic.WebApp.Components.Common;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace AxlProtocolMusic.WebApp.Tests.Components.Common;

[TestFixture]
public sealed class PageAnalyticsTrackerTests
{
    [Test]
    public void PageAnalyticsTracker_OnFirstRender_ImportsModuleAndStartsTrackingCurrentPage()
    {
        using var context = new BunitContext();
        var module = new FakeJsObjectReference();
        var jsRuntime = new FakeJsRuntime(module);
        context.Services.AddSingleton<IJSRuntime>(jsRuntime);
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/news");

        context.Render<PageAnalyticsTracker>();

        Assert.That(jsRuntime.ImportCalls, Is.EqualTo(["/js/pageAnalytics.js"]));
        Assert.That(module.IdentifierCalls, Does.Contain("startTracking"));
        Assert.That(module.LastStringArgumentByIdentifier["startTracking"], Is.EqualTo("http://localhost/news"));
    }

    [Test]
    public void PageAnalyticsTracker_WhenNavigationChanges_TracksNavigation()
    {
        using var context = new BunitContext();
        var module = new FakeJsObjectReference();
        var jsRuntime = new FakeJsRuntime(module);
        context.Services.AddSingleton<IJSRuntime>(jsRuntime);
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var cut = context.Render<PageAnalyticsTracker>();

        navigation.NavigateTo("/timeline?year=2026");

        cut.WaitForAssertion(() =>
        {
            Assert.That(module.IdentifierCalls, Does.Contain("trackNavigation"));
            Assert.That(module.LastStringArgumentByIdentifier["trackNavigation"], Is.EqualTo("http://localhost/timeline?year=2026"));
        });
    }

    [Test]
    public void PageAnalyticsTracker_OnSubsequentRender_SyncsCurrentTitle()
    {
        using var context = new BunitContext();
        var module = new FakeJsObjectReference();
        var jsRuntime = new FakeJsRuntime(module);
        context.Services.AddSingleton<IJSRuntime>(jsRuntime);
        var cut = context.Render<PageAnalyticsTracker>();

        cut.Render();

        Assert.That(module.IdentifierCalls.Count(call => call == "syncCurrentTitle"), Is.EqualTo(1));
    }

    [Test]
    public async Task PageAnalyticsTracker_DisposeAsync_DisposesModuleAndStopsTrackingFurtherNavigation()
    {
        using var context = new BunitContext();
        var module = new FakeJsObjectReference();
        var jsRuntime = new FakeJsRuntime(module);
        context.Services.AddSingleton<IJSRuntime>(jsRuntime);
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var cut = context.Render<PageAnalyticsTracker>();

        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        navigation.NavigateTo("/privacy");

        Assert.That(module.DisposeCalls, Is.EqualTo(1));
        Assert.That(module.IdentifierCalls.Count(call => call == "trackNavigation"), Is.EqualTo(0));
    }

    [Test]
    public async Task PageAnalyticsTracker_WhenJsDisconnectsDuringSyncOrDispose_SwallowsException()
    {
        using var context = new BunitContext();
        var module = new FakeJsObjectReference
        {
            ThrowOnSyncCurrentTitle = true,
            ThrowOnDispose = true
        };
        var jsRuntime = new FakeJsRuntime(module);
        context.Services.AddSingleton<IJSRuntime>(jsRuntime);
        var cut = context.Render<PageAnalyticsTracker>();

        Assert.DoesNotThrow(() => cut.Render());
        Assert.DoesNotThrowAsync(async () => await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask()));
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        private readonly FakeJsObjectReference _module;

        public FakeJsRuntime(FakeJsObjectReference module)
        {
            _module = module;
        }

        public List<string> ImportCalls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier != "import")
            {
                throw new NotSupportedException($"Unexpected runtime identifier: {identifier}");
            }

            ImportCalls.Add(args?.Single()?.ToString() ?? string.Empty);
            return new ValueTask<TValue>((TValue)(object)_module);
        }
    }

    private sealed class FakeJsObjectReference : IJSObjectReference
    {
        public List<string> IdentifierCalls { get; } = [];

        public Dictionary<string, string> LastStringArgumentByIdentifier { get; } = [];

        public int DisposeCalls { get; private set; }

        public bool ThrowOnSyncCurrentTitle { get; set; }

        public bool ThrowOnTrackNavigation { get; set; }

        public bool ThrowOnDispose { get; set; }

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;

            if (ThrowOnDispose)
            {
                throw new JSDisconnectedException("Disconnected");
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            IdentifierCalls.Add(identifier);

            var stringArgument = args?.OfType<string>().SingleOrDefault();
            if (!string.IsNullOrEmpty(stringArgument))
            {
                LastStringArgumentByIdentifier[identifier] = stringArgument;
            }

            if (identifier == "syncCurrentTitle" && ThrowOnSyncCurrentTitle)
            {
                throw new JSDisconnectedException("Disconnected");
            }

            if (identifier == "trackNavigation" && ThrowOnTrackNavigation)
            {
                throw new JSDisconnectedException("Disconnected");
            }

            if (typeof(TValue) == typeof(IJSObjectReference))
            {
                return new ValueTask<TValue>((TValue)(object)this);
            }

            return typeof(TValue) == typeof(JsonElement)
                ? new ValueTask<TValue>((TValue)(object)default(JsonElement))
                : new ValueTask<TValue>(default(TValue)!);
        }
    }
}
