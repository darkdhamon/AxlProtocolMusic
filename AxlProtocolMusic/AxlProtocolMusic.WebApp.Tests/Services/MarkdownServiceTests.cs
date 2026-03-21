using AxlProtocolMusic.WebApp.Services;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class MarkdownServiceTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ToHtml_WhenMarkdownIsBlank_ReturnsEmptyString(string? markdown)
    {
        var service = new MarkdownService();

        var result = service.ToHtml(markdown);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ToHtml_WhenMarkdownContainsFormatting_RendersExpectedHtml()
    {
        var service = new MarkdownService();

        var result = service.ToHtml("## Title\n\n**Bold** and _italic_");

        Assert.That(result, Does.Contain("<h2"));
        Assert.That(result, Does.Contain(">Title</h2>"));
        Assert.That(result, Does.Contain("<strong>Bold</strong>"));
        Assert.That(result, Does.Contain("<em>italic</em>"));
    }

    [Test]
    public void ToHtml_WhenMarkdownContainsHtml_DoesNotRenderRawHtml()
    {
        var service = new MarkdownService();

        var result = service.ToHtml("<script>alert('x')</script>\n\nParagraph");

        Assert.That(result, Does.Not.Contain("<script>"));
        Assert.That(result, Does.Contain("&lt;script&gt;alert('x')&lt;/script&gt;"));
        Assert.That(result, Does.Contain("<p>Paragraph</p>"));
    }
}
