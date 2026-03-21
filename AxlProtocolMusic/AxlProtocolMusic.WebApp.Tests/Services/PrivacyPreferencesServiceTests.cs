using System.Text.Json;
using AxlProtocolMusic.WebApp.Models.Privacy;
using AxlProtocolMusic.WebApp.Services;
using Microsoft.JSInterop;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class PrivacyPreferencesServiceTests
{
    [Test]
    public async Task GetAsync_ImportsModuleOnceAndReturnsPreferences()
    {
        var jsModule = new FakeJsObjectReference();
        var jsRuntime = new FakeJsRuntime(jsModule);
        jsModule.SetResult(
            "getPreferences",
            new PrivacyPreferences
            {
                AllowEssentialSiteMetrics = false,
                ShareApproximateLocation = true
            });

        await using var service = new PrivacyPreferencesService(jsRuntime);

        var first = await service.GetAsync();
        var second = await service.GetAsync();

        Assert.That(first.AllowEssentialSiteMetrics, Is.False);
        Assert.That(first.ShareApproximateLocation, Is.True);
        Assert.That(second.AllowEssentialSiteMetrics, Is.False);
        Assert.That(jsRuntime.ImportCalls, Is.EqualTo(new[] { "/js/privacyPreferences.js" }));
        Assert.That(jsModule.IdentifierCalls.Count(call => call == "getPreferences"), Is.EqualTo(2));
    }

    [Test]
    public async Task SaveAsync_InvokesSavePreferencesWithPreferences()
    {
        var jsModule = new FakeJsObjectReference();
        var jsRuntime = new FakeJsRuntime(jsModule);
        var preferences = new PrivacyPreferences
        {
            AllowEssentialSiteMetrics = true,
            AllowEnhancedEngagementMetrics = true
        };

        jsModule.SetResult(
            "savePreferences",
            new PrivacyPreferenceSaveResult
            {
                Preferences = preferences
            });

        await using var service = new PrivacyPreferencesService(jsRuntime);

        var result = await service.SaveAsync(preferences);

        Assert.That(result.Preferences.AllowEnhancedEngagementMetrics, Is.True);
        Assert.That(jsModule.IdentifierCalls, Does.Contain("savePreferences"));
        Assert.That(jsModule.LastPreferencesArgument, Is.Not.Null);
        Assert.That(jsModule.LastPreferencesArgument!.AllowEnhancedEngagementMetrics, Is.True);
    }

    [Test]
    public async Task SyncApproximateLocationPreferenceAsync_InvokesExpectedModuleMethod()
    {
        var jsModule = new FakeJsObjectReference();
        var jsRuntime = new FakeJsRuntime(jsModule);
        jsModule.SetResult(
            "syncApproximateLocationPreference",
            new PrivacyPreferenceSaveResult
            {
                Preferences = new PrivacyPreferences
                {
                    ShareApproximateLocation = true
                }
            });

        await using var service = new PrivacyPreferencesService(jsRuntime);

        var result = await service.SyncApproximateLocationPreferenceAsync(
            new PrivacyPreferences
            {
                ShareApproximateLocation = true
            });

        Assert.That(result.Preferences.ShareApproximateLocation, Is.True);
        Assert.That(jsModule.IdentifierCalls, Does.Contain("syncApproximateLocationPreference"));
    }

    [Test]
    public async Task DisposeAsync_WhenModuleWasLoaded_DisposesModule()
    {
        var jsModule = new FakeJsObjectReference();
        var jsRuntime = new FakeJsRuntime(jsModule);
        jsModule.SetResult("getPreferences", new PrivacyPreferences());
        await using var service = new PrivacyPreferencesService(jsRuntime);

        await service.GetAsync();
        await service.DisposeAsync();

        Assert.That(jsModule.DisposeCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task DisposeAsync_WhenModuleThrowsDisconnectedException_SwallowsIt()
    {
        var jsModule = new FakeJsObjectReference
        {
            ThrowOnDispose = true
        };

        var jsRuntime = new FakeJsRuntime(jsModule);
        jsModule.SetResult("getPreferences", new PrivacyPreferences());
        await using var service = new PrivacyPreferencesService(jsRuntime);

        await service.GetAsync();

        Assert.DoesNotThrowAsync(async () => await service.DisposeAsync().AsTask());
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
        private readonly Dictionary<string, object> _results = [];

        public List<string> IdentifierCalls { get; } = [];

        public PrivacyPreferences? LastPreferencesArgument { get; private set; }

        public int DisposeCalls { get; private set; }

        public bool ThrowOnDispose { get; set; }

        public void SetResult<T>(string identifier, T value)
        {
            _results[identifier] = value!;
        }

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
            LastPreferencesArgument = args?.OfType<PrivacyPreferences>().SingleOrDefault();

            if (!_results.TryGetValue(identifier, out var result))
            {
                throw new NotSupportedException($"No result configured for identifier '{identifier}'.");
            }

            if (result is JsonElement jsonElement)
            {
                return new ValueTask<TValue>(jsonElement.Deserialize<TValue>()!);
            }

            return new ValueTask<TValue>((TValue)result);
        }
    }
}
