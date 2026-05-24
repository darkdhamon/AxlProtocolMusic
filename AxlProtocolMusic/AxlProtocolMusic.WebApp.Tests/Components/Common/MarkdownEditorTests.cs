using AxlProtocolMusic.WebApp.Components.Common;
using AxlProtocolMusic.WebApp.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace AxlProtocolMusic.WebApp.Tests.Components.Common;

[TestFixture]
public sealed class MarkdownEditorTests
{
    [Test]
    public void MarkdownEditor_WhenValueIsEmpty_ShowsEmptyPreviewState()
    {
        using var context = CreateContext();

        var cut = context.Render<MarkdownEditor>();

        Assert.That(cut.Markup, Does.Contain("Formatting preview will appear here."));
        Assert.That(cut.Markup, Does.Not.Contain("markdown-preview-body"));
    }

    [Test]
    public void MarkdownEditor_WhenValueIsProvided_RendersMarkdownPreview()
    {
        using var context = CreateContext();

        var cut = context.Render<MarkdownEditor>(parameters => parameters
            .Add(component => component.Value, "**Hello**"));

        Assert.That(cut.Markup, Does.Contain("<strong>Hello</strong>"));
        Assert.That(cut.Markup, Does.Contain("markdown-preview-body"));
    }

    [Test]
    public void MarkdownEditor_WhenLyricsPreviewIsEnabled_UsesLyricsPreviewClasses()
    {
        using var context = CreateContext();

        var cut = context.Render<MarkdownEditor>(parameters => parameters
            .Add(component => component.UseLyricsPreviewStyle, true)
            .Add(component => component.Value, "Line one"));

        Assert.That(cut.Markup, Does.Contain("markdown-preview markdown-preview-lyrics"));
        Assert.That(cut.Markup, Does.Contain("markdown-preview-body markdown-preview-body-lyrics song-lyrics-markdown"));
    }

    [Test]
    public void MarkdownEditor_WhenInputChanges_InvokesValueChanged()
    {
        using var context = CreateContext();
        string? capturedValue = null;

        var cut = context.Render<MarkdownEditor>(parameters => parameters
            .Add(component => component.Value, string.Empty)
            .Add(component => component.ValueChanged, value => capturedValue = value));

        cut.Find("textarea").Input("Updated markdown");

        Assert.That(capturedValue, Is.EqualTo("Updated markdown"));
    }

    [Test]
    public void MarkdownEditor_WhenToolbarButtonsAreClicked_ImportsModuleOnceAndAppliesFormats()
    {
        using var context = CreateContext(out var jsRuntime, out var jsModule);

        var cut = context.Render<MarkdownEditor>();

        var buttons = cut.FindAll(".markdown-toolbar button");
        buttons[0].Click();
        buttons[1].Click();
        buttons[5].Click();

        Assert.That(jsRuntime.ImportCalls, Is.EqualTo(["/js/markdownEditor.js"]));
        Assert.That(jsModule.FormatsApplied, Is.EqualTo(["heading", "bold", "link"]));
    }

    [Test]
    public async Task MarkdownEditor_DisposeAsync_WhenModuleWasLoaded_DisposesModule()
    {
        using var context = CreateContext(out _, out var jsModule);

        var cut = context.Render<MarkdownEditor>();
        cut.FindAll(".markdown-toolbar button")[0].Click();

        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        Assert.That(jsModule.DisposeCalls, Is.EqualTo(1));
    }

    private static BunitContext CreateContext()
    {
        return CreateContext(out _, out _);
    }

    private static BunitContext CreateContext(out FakeJsRuntime jsRuntime, out FakeJsObjectReference jsModule)
    {
        var context = new BunitContext();
        jsModule = new FakeJsObjectReference();
        jsRuntime = new FakeJsRuntime(jsModule);
        context.Services.AddSingleton<IJSRuntime>(jsRuntime);
        context.Services.AddSingleton<MarkdownService>();
        return context;
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
        public List<string> FormatsApplied { get; } = [];

        public int DisposeCalls { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "applyMarkdownFormat")
            {
                var format = args?.Skip(1).OfType<string>().SingleOrDefault();
                if (!string.IsNullOrWhiteSpace(format))
                {
                    FormatsApplied.Add(format);
                }
            }

            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
