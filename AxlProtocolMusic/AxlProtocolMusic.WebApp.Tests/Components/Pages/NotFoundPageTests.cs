using AxlProtocolMusic.WebApp.Components.Pages;
using Bunit;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class NotFoundPageTests
{
    [Test]
    public void NotFound_RendersCustom404Content()
    {
        using var context = new BunitContext();

        var cut = context.Render<NotFound>();

        Assert.That(cut.Markup, Does.Contain("404 Error"));
        Assert.That(cut.Markup, Does.Contain("Page not found"));
        Assert.That(cut.Markup, Does.Contain("The page you requested does not exist or may have moved."));
        Assert.That(cut.Markup, Does.Contain("Use the site navigation to get back to the section you were looking for."));
        Assert.That(cut.Markup, Does.Not.Contain("You Might Like"));
        Assert.That(cut.Markup, Does.Not.Contain("View Article"));
        Assert.That(cut.Markup, Does.Not.Contain("View Release"));

        var image = cut.Find("img");
        Assert.That(image.GetAttribute("src"), Is.EqualTo("/Assets/Misc/404-Graphic.png"));
        Assert.That(image.GetAttribute("alt"), Is.EqualTo("Stylized 404 graphic"));
    }
}
