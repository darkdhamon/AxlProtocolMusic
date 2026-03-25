using System.Text.Json;
using AxlProtocolMusic.WebApp.Components.Common;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace AxlProtocolMusic.WebApp.Tests.Components.Common;

[TestFixture]
public sealed class PrivacyLocationMapTests
{
    [Test]
    public void PrivacyLocationMap_WhenLocationsExist_RendersNormalizedLabelsAndLegend()
    {
        using var context = CreateContext(out _, explicitStyleUrl: "https://maps.example/style.json");

        var cut = context.Render<PrivacyLocationMap>(parameters => parameters
            .Add(component => component.Locations,
            [
                new PrivacyLocationMap.PrivacyMapLocation
                {
                    Label = "Township of Hamilton, New Jersey, United States of America (the)",
                    Latitude = 40.0,
                    Longitude = -74.0,
                    Count = 4
                },
                new PrivacyLocationMap.PrivacyMapLocation
                {
                    Label = "Austin Township, Texas, United States",
                    Latitude = 30.0,
                    Longitude = -97.0,
                    Count = 2
                },
                new PrivacyLocationMap.PrivacyMapLocation
                {
                    Label = "",
                    Latitude = 0,
                    Longitude = 0,
                    Count = 1
                },
                new PrivacyLocationMap.PrivacyMapLocation
                {
                    Label = "London, England, United Kingdom of Great Britain and Northern Ireland (the)",
                    Latitude = 51.5,
                    Longitude = -0.12,
                    Count = 3
                }
            ]));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("4 locations plotted"));
            Assert.That(cut.Markup, Does.Contain("Focus: Hamilton, NJ, USA"));
            Assert.That(cut.Markup, Does.Contain("Hamilton, NJ, USA"));
            Assert.That(cut.Markup, Does.Contain("Austin, TX, USA"));
            Assert.That(cut.Markup, Does.Contain("Region Unknown"));
            Assert.That(cut.Markup, Does.Not.Contain("London, England, UK"));
        });
    }

    [Test]
    public void PrivacyLocationMap_OnFirstRender_ImportsModuleAndUsesExplicitStyleUrl()
    {
        using var context = CreateContext(out var module, explicitStyleUrl: "https://maps.example/style.json");
        var locations =
            new[]
            {
                new PrivacyLocationMap.PrivacyMapLocation
                {
                    Label = "Madison, Wisconsin, United States",
                    Latitude = 43.07,
                    Longitude = -89.4,
                    Count = 2
                }
            };

        context.Render<PrivacyLocationMap>(parameters => parameters.Add(component => component.Locations, locations));

        Assert.That(module.IdentifierCalls, Does.Contain("renderPrivacyLocationMap"));
        Assert.That(module.LastRenderStyleUrl, Is.EqualTo("https://maps.example/style.json"));
        Assert.That(module.LastRenderLocations, Has.Count.EqualTo(1));
        Assert.That(module.LastRenderLocations[0].Label, Is.EqualTo("Madison, Wisconsin, United States"));
        Assert.That(module.LastRenderMapElementId, Does.StartWith("privacy-location-map-"));
    }

    [Test]
    public void PrivacyLocationMap_WhenOnlyMapTilerKeyExists_UsesGeneratedStyleUrl()
    {
        using var context = CreateContext(out var module, mapTilerKey: "abc 123");

        context.Render<PrivacyLocationMap>(parameters => parameters.Add(component => component.Locations, Array.Empty<PrivacyLocationMap.PrivacyMapLocation>()));

        Assert.That(module.LastRenderStyleUrl, Is.EqualTo("https://api.maptiler.com/maps/streets-v2/style.json?key=abc%20123"));
    }

    [Test]
    public async Task PrivacyLocationMap_WhenJsDisconnectsDuringRenderOrDispose_SwallowsException()
    {
        using var context = CreateContext(out var module, explicitStyleUrl: "https://maps.example/style.json");
        module.ThrowOnRender = true;
        module.ThrowOnDispose = true;

        var cut = context.Render<PrivacyLocationMap>(parameters => parameters
            .Add(component => component.Locations,
            [
                new PrivacyLocationMap.PrivacyMapLocation
                {
                    Label = "Madison, Wisconsin, United States",
                    Latitude = 43.07,
                    Longitude = -89.4,
                    Count = 2
                }
            ]));

        Assert.DoesNotThrow(() => cut.Render());
        Assert.DoesNotThrowAsync(async () => await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask()));
    }

    private static BunitContext CreateContext(
        out FakeJsObjectReference module,
        string? explicitStyleUrl = null,
        string? mapTilerKey = null)
    {
        var context = new BunitContext();
        module = new FakeJsObjectReference();
        context.Services.AddSingleton<IJSRuntime>(new FakeJsRuntime(module));
        context.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MapSettings:PrivacyLocationStyleUrl"] = explicitStyleUrl,
                ["MapSettings:MapTilerKey"] = mapTilerKey
            })
            .Build());
        return context;
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        private readonly FakeJsObjectReference _module;

        public FakeJsRuntime(FakeJsObjectReference module)
        {
            _module = module;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier != "import")
            {
                throw new NotSupportedException($"Unexpected runtime identifier: {identifier}");
            }

            return new ValueTask<TValue>((TValue)(object)_module);
        }
    }

    private sealed class FakeJsObjectReference : IJSObjectReference
    {
        public List<string> IdentifierCalls { get; } = [];

        public string? LastRenderMapElementId { get; private set; }

        public IReadOnlyList<PrivacyLocationMap.PrivacyMapLocation> LastRenderLocations { get; private set; } = [];

        public string? LastRenderStyleUrl { get; private set; }

        public bool ThrowOnRender { get; set; }

        public bool ThrowOnDispose { get; set; }

        public ValueTask DisposeAsync()
        {
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

            if (identifier == "renderPrivacyLocationMap")
            {
                LastRenderMapElementId = args?[0]?.ToString();
                LastRenderLocations = (args?[1] as IReadOnlyList<PrivacyLocationMap.PrivacyMapLocation>) ?? [];
                LastRenderStyleUrl = args?[2]?.ToString();

                if (ThrowOnRender)
                {
                    throw new JSDisconnectedException("Disconnected");
                }
            }

            return typeof(TValue) == typeof(JsonElement)
                ? new ValueTask<TValue>((TValue)(object)default(JsonElement))
                : new ValueTask<TValue>(default(TValue)!);
        }
    }
}
