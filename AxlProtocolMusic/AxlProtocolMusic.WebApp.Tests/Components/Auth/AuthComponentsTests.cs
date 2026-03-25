using AxlProtocolMusic.WebApp.Components.Auth;
using AxlProtocolMusic.WebApp.Components.Pages;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AxlProtocolMusic.WebApp.Tests.Components.Auth;

[TestFixture]
public sealed class AuthComponentsTests
{
    [Test]
    public void RedirectToLogin_WhenUserIsAnonymous_NavigatesToLoginWithReturnUrl()
    {
        using var context = new Bunit.BunitContext();
        var authContext = context.AddAuthorization();
        authContext.SetNotAuthorized();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("https://localhost/releases?filter=new");

        context.Render<RedirectToLogin>();

        Assert.That(navigation.Uri, Is.EqualTo("https://localhost/login?returnUrl=%2Freleases%3Ffilter%3Dnew"));
    }

    [Test]
    public void RedirectToLogin_WhenUserIsAuthenticated_NavigatesToAccessDenied()
    {
        using var context = new Bunit.BunitContext();
        var authContext = context.AddAuthorization();
        authContext.SetAuthorized("admin");
        var navigation = context.Services.GetRequiredService<NavigationManager>();

        context.Render<RedirectToLogin>();

        Assert.That(navigation.Uri, Does.EndWith("/access-denied"));
    }

    [Test]
    public void Login_WhenUserIsAnonymous_RendersFormAndMessages()
    {
        using var context = new Bunit.BunitContext();
        var authContext = context.AddAuthorization();
        authContext.SetNotAuthorized();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/login?ReturnUrl=%2Fnews&Error=Bad%20credentials&Success=Password%20updated");

        var cut = context.Render<Login>();

        Assert.That(cut.Find("h1").TextContent, Is.EqualTo("Admin Login"));
        Assert.That(cut.Markup, Does.Contain("Bad credentials"));
        Assert.That(cut.Markup, Does.Contain("Password updated"));
        Assert.That(cut.Find("form").GetAttribute("action"), Is.EqualTo("/account/login"));
        Assert.That(cut.Find("input[type='hidden'][name='ReturnUrl']").GetAttribute("value"), Is.EqualTo("/news"));
        Assert.That(cut.Find("#username").GetAttribute("name"), Is.EqualTo("UserNameOrEmail"));
        Assert.That(cut.Find("#password").GetAttribute("name"), Is.EqualTo("Password"));
        Assert.That(cut.Find("#rememberMe").GetAttribute("name"), Is.EqualTo("RememberMe"));
        Assert.That(cut.Find("button[type='submit']").TextContent, Is.EqualTo("Log In"));
    }

    [Test]
    public void Login_WhenReturnUrlIsUnsafe_FallsBackToAdmin()
    {
        using var context = new Bunit.BunitContext();
        var authContext = context.AddAuthorization();
        authContext.SetNotAuthorized();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/login?ReturnUrl=https%3A%2F%2Fevil.example%2Fphish");

        var cut = context.Render<Login>();

        var hiddenInput = cut.Find("input[type='hidden'][name='ReturnUrl']");
        Assert.That(hiddenInput.GetAttribute("value"), Is.EqualTo("/admin"));
    }

    [Test]
    public void AccessDenied_RendersExpectedMessage()
    {
        using var context = new Bunit.BunitContext();

        var cut = context.Render<AccessDenied>();

        Assert.That(cut.Find("h1").TextContent, Is.EqualTo("Access Denied"));
        Assert.That(cut.Markup, Does.Contain("You are signed in, but you do not have permission to view this area."));
    }
}
