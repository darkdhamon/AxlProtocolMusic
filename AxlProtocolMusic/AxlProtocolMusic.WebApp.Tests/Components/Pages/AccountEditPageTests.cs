using AxlProtocolMusic.WebApp.Components.Pages;
using AxlProtocolMusic.WebApp.Models.Identity;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class AccountEditPageTests
{
    [Test]
    public void AccountEdit_WhenUserExists_RendersAccountFormAndAlerts()
    {
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");

        var userManager = CreateUserManager(new ApplicationUser
        {
            UserName = "admin-user",
            Email = "admin@example.com"
        });
        context.Services.AddSingleton(userManager.Object);
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/account/edit?Error=Bad%20password&Success=Saved&ForcePasswordChange=true");

        var cut = context.Render<AccountEdit>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Edit Account"));
            Assert.That(cut.Markup, Does.Contain("default password"));
            Assert.That(cut.Markup, Does.Contain("Bad password"));
            Assert.That(cut.Markup, Does.Contain("Saved"));
            Assert.That(cut.Find("#username").GetAttribute("value"), Is.EqualTo("admin-user"));
            Assert.That(cut.Find("#email").GetAttribute("value"), Is.EqualTo("admin@example.com"));
            Assert.That(cut.Find("form").GetAttribute("action"), Is.EqualTo("/account/update"));
        });
    }

    [Test]
    public void AccountEdit_WhenNoUserIsResolved_ShowsLoadingState()
    {
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");

        var userManager = CreateUserManager(null);
        context.Services.AddSingleton(userManager.Object);

        var cut = context.Render<AccountEdit>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Loading account details..."));
        });
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager(ApplicationUser? user)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var manager = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        manager
            .Setup(instance => instance.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        return manager;
    }
}
