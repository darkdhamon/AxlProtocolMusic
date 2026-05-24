using AxlProtocolMusic.WebApp.Components.Layout;
using Bunit;
using Bunit.TestDoubles;

namespace AxlProtocolMusic.WebApp.Tests.Components.Layout;

[TestFixture]
public sealed class NavMenuTests
{
    [Test]
    public void NavMenu_WhenUserIsAnonymous_RendersPublicNavigationOnly()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetNotAuthorized();

        var cut = context.Render<NavMenu>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Axl Protocol Music home"));
            Assert.That(cut.Markup, Does.Contain("Home"));
            Assert.That(cut.Markup, Does.Contain("Releases"));
            Assert.That(cut.Markup, Does.Contain("News Articles"));
            Assert.That(cut.Markup, Does.Contain("About Axl Protocol"));
            Assert.That(cut.Markup, Does.Contain("Timeline"));
            Assert.That(cut.Markup, Does.Contain("Privacy"));
            Assert.That(cut.Markup, Does.Not.Contain("> Admin<"));
            Assert.That(cut.Markup, Does.Not.Contain("> Account<"));
            Assert.That(cut.Markup, Does.Not.Contain("Log out"));
        });
    }

    [Test]
    public void NavMenu_WhenUserIsAuthorized_RendersAdminAccountAndLogoutActions()
    {
        using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("admin");

        var cut = context.Render<NavMenu>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain(" Admin"));
            Assert.That(cut.Markup, Does.Contain(" Account"));
            Assert.That(cut.Markup, Does.Contain("method=\"post\""));
            Assert.That(cut.Markup, Does.Contain("action=\"/account/logout\""));
            Assert.That(cut.Markup, Does.Contain("Log out"));
        });
    }
}
