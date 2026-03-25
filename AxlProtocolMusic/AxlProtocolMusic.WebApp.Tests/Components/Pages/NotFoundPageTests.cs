using AxlProtocolMusic.WebApp.Components.Pages;
using Bunit;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class NotFoundPageTests
{
    [Test]
    public void NotFound_RendersExpectedMessage()
    {
        using var context = new BunitContext();

        var cut = context.Render<NotFound>();

        Assert.That(cut.Markup, Does.Contain("Not Found"));
        Assert.That(cut.Markup, Does.Contain("Sorry, the content you are looking for does not exist."));
    }
}
