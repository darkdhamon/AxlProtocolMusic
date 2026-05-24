using System.Security.Claims;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Controllers;
using AxlProtocolMusic.WebApp.Models.Authentication;
using AxlProtocolMusic.WebApp.Models.Analytics;
using AxlProtocolMusic.WebApp.Models.Identity;
using AxlProtocolMusic.WebApp.Services.Development;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace AxlProtocolMusic.WebApp.Tests.Controllers;

[TestFixture]
public sealed class AccountControllerTests
{
    [Test]
    public async Task Login_WhenModelStateIsInvalid_RedirectsToLoginWithValidationMessage()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(signInManager, userManager, analyticsService);
        controller.ModelState.AddModelError("Password", "Required");

        var result = await controller.Login(new LoginRequest
        {
            UserNameOrEmail = "admin",
            ReturnUrl = "/news"
        });

        var redirect = result as RedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/login?error=Enter%20both%20a%20username%20or%20email%20and%20password.&returnUrl=%2Fnews"));
        signInManager.Verify(instance => instance.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), true), Times.Never);
    }

    [Test]
    public async Task Login_WhenUserCannotBeResolved_RedirectsToLogin()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        userManager.Setup(instance => instance.FindByNameAsync("missing")).ReturnsAsync((ApplicationUser?)null);
        var controller = CreateController(signInManager, userManager, analyticsService);

        var result = await controller.Login(new LoginRequest
        {
            UserNameOrEmail = "missing",
            Password = "bad-password",
            ReturnUrl = "/admin"
        });

        var redirect = result as RedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/login?error=Invalid%20login%20attempt.&returnUrl=%2Fadmin"));
        signInManager.Verify(instance => instance.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), true), Times.Never);
    }

    [Test]
    public async Task Login_WhenSuccessfulAndUsingDefaultPassword_DeletesVisitorMetricsAndRedirectsToForcedPasswordChange()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        var user = new ApplicationUser
        {
            UserName = "admin-user",
            Email = "admin@example.com"
        };

        userManager.Setup(instance => instance.FindByNameAsync("admin-user")).ReturnsAsync(user);
        userManager.Setup(instance => instance.CheckPasswordAsync(user, "secret-password")).ReturnsAsync(true);
        signInManager
            .Setup(instance => instance.PasswordSignInAsync("admin-user", "secret-password", true, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var controller = CreateController(signInManager, userManager, analyticsService, bootstrapPassword: "secret-password", isHttps: true);
        controller.HttpContext.Request.Headers.Cookie = "axl_visitor_id=visitor-123";

        var result = await controller.Login(new LoginRequest
        {
            UserNameOrEmail = "admin-user",
            Password = "secret-password",
            RememberMe = true,
            ReturnUrl = "/releases"
        });

        var redirect = result as LocalRedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/account/edit?forcePasswordChange=true"));
        Assert.That(analyticsService.DeletedVisitorIds, Is.EqualTo(["visitor-123"]));

        var setCookieHeader = controller.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.That(setCookieHeader, Does.Contain("axl_admin_visitor=true"));
        Assert.That(setCookieHeader, Does.Contain("httponly"));
        Assert.That(setCookieHeader, Does.Contain("secure"));
    }

    [Test]
    public async Task Login_WhenSuccessfulWithUnsafeReturnUrl_RedirectsToAdmin()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        var user = new ApplicationUser
        {
            UserName = "admin-user"
        };

        userManager.Setup(instance => instance.FindByNameAsync("admin-user")).ReturnsAsync(user);
        userManager.Setup(instance => instance.CheckPasswordAsync(user, "entered-password")).ReturnsAsync(false);
        signInManager
            .Setup(instance => instance.PasswordSignInAsync("admin-user", "entered-password", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var controller = CreateController(signInManager, userManager, analyticsService);
        controller.Url = CreateUrlHelper(isLocalUrl: false);

        var result = await controller.Login(new LoginRequest
        {
            UserNameOrEmail = "admin-user",
            Password = "entered-password",
            ReturnUrl = "https://evil.example/phish"
        });

        var redirect = result as LocalRedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/admin"));
    }

    [Test]
    public async Task Login_WhenAccountIsLockedOut_RedirectsWithLockoutMessage()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        var user = new ApplicationUser
        {
            UserName = "admin-user"
        };

        userManager.Setup(instance => instance.FindByNameAsync("admin-user")).ReturnsAsync(user);
        signInManager
            .Setup(instance => instance.PasswordSignInAsync("admin-user", "bad-password", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var controller = CreateController(signInManager, userManager, analyticsService);

        var result = await controller.Login(new LoginRequest
        {
            UserNameOrEmail = "admin-user",
            Password = "bad-password"
        });

        var redirect = result as RedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/login?error=This%20account%20is%20locked.%20Try%20again%20later.&returnUrl=%2Fadmin"));
    }

    [Test]
    public async Task Logout_SignsOutAndRedirectsHome()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(signInManager, userManager, analyticsService);

        var result = await controller.Logout();

        var redirect = result as LocalRedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/"));
        signInManager.Verify(instance => instance.SignOutAsync(), Times.Once);
    }

    [Test]
    public async Task ResetDevelopmentDatabase_WhenNotInDevelopment_ReturnsNotFound()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(signInManager, userManager, analyticsService, isDevelopment: false);

        var result = await controller.ResetDevelopmentDatabase();

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        signInManager.Verify(instance => instance.SignOutAsync(), Times.Never);
    }

    [Test]
    public async Task Update_WhenModelStateIsInvalid_RedirectsToAccountEditWithValidationError()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(signInManager, userManager, analyticsService);
        controller.ModelState.AddModelError("Email", "Required");

        var result = await controller.Update(new AccountUpdateRequest());

        var redirect = result as RedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/account/edit?error=Complete%20all%20required%20account%20fields."));
    }

    [Test]
    public async Task Update_WhenPasswordConfirmationDoesNotMatch_RedirectsToAccountEdit()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        var controller = CreateController(signInManager, userManager, analyticsService);

        var result = await controller.Update(new AccountUpdateRequest
        {
            UserName = "admin-user",
            Email = "admin@example.com",
            CurrentPassword = "current-password",
            NewPassword = "new-password",
            ConfirmNewPassword = "different-password"
        });

        var redirect = result as RedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/account/edit?error=New%20password%20and%20confirmation%20do%20not%20match."));
    }

    [Test]
    public async Task Update_WhenUserCannotBeResolved_SignsOutAndRedirectsToLogin()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        userManager.Setup(instance => instance.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);
        var controller = CreateController(signInManager, userManager, analyticsService);

        var result = await controller.Update(new AccountUpdateRequest
        {
            UserName = "admin-user",
            Email = "admin@example.com",
            CurrentPassword = "current-password"
        });

        var redirect = result as LocalRedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/login?error=Your session expired. Please log in again."));
        signInManager.Verify(instance => instance.SignOutAsync(), Times.Once);
    }

    [Test]
    public async Task Update_WhenCurrentPasswordIsIncorrect_RedirectsToAccountEdit()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        var user = new ApplicationUser
        {
            UserName = "admin-user",
            Email = "admin@example.com"
        };

        userManager.Setup(instance => instance.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(instance => instance.CheckPasswordAsync(user, "wrong-password")).ReturnsAsync(false);
        var controller = CreateController(signInManager, userManager, analyticsService);

        var result = await controller.Update(new AccountUpdateRequest
        {
            UserName = "admin-user",
            Email = "admin@example.com",
            CurrentPassword = "wrong-password"
        });

        var redirect = result as RedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/account/edit?error=Current%20password%20is%20incorrect."));
    }

    [Test]
    public async Task Update_WhenUsernameUpdateFails_RedirectsToAccountEditWithIdentityError()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        var user = new ApplicationUser
        {
            UserName = "old-user",
            Email = "old@example.com"
        };

        userManager.Setup(instance => instance.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(instance => instance.CheckPasswordAsync(user, "current-password")).ReturnsAsync(true);
        userManager
            .Setup(instance => instance.SetUserNameAsync(user, "new-user"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Username already taken." }));
        var controller = CreateController(signInManager, userManager, analyticsService);

        var result = await controller.Update(new AccountUpdateRequest
        {
            UserName = " new-user ",
            Email = " new@example.com ",
            CurrentPassword = "current-password"
        });

        var redirect = result as RedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/account/edit?error=Username%20already%20taken."));
    }

    [Test]
    public async Task Update_WhenRequestIsValid_UpdatesAccountAndRefreshesSignIn()
    {
        var signInManager = CreateSignInManager();
        var userManager = CreateUserManager();
        var analyticsService = new FakeAnalyticsService();
        var user = new ApplicationUser
        {
            UserName = "old-user",
            Email = "old@example.com"
        };

        userManager.Setup(instance => instance.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(instance => instance.CheckPasswordAsync(user, "current-password")).ReturnsAsync(true);
        userManager.Setup(instance => instance.SetUserNameAsync(user, "new-user")).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(instance => instance.SetEmailAsync(user, "new@example.com")).ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(instance => instance.ChangePasswordAsync(user, "current-password", "new-password"))
            .ReturnsAsync(IdentityResult.Success);
        var controller = CreateController(signInManager, userManager, analyticsService);

        var result = await controller.Update(new AccountUpdateRequest
        {
            UserName = " new-user ",
            Email = " new@example.com ",
            CurrentPassword = "current-password",
            NewPassword = "new-password",
            ConfirmNewPassword = "new-password"
        });

        var redirect = result as RedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/account/edit?success=Account%20details%20updated."));
        Assert.That(user.UserName, Is.EqualTo("new-user"));
        Assert.That(user.Email, Is.EqualTo("new@example.com"));
        signInManager.Verify(instance => instance.RefreshSignInAsync(user), Times.Once);
    }

    private static AccountController CreateController(
        Mock<SignInManager<ApplicationUser>> signInManager,
        Mock<UserManager<ApplicationUser>> userManager,
        FakeAnalyticsService analyticsService,
        string bootstrapPassword = "default-password",
        bool isDevelopment = true,
        bool isHttps = false)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = isHttps ? "https" : "http";
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "admin"),
            new Claim(ClaimTypes.Name, "admin-user")
        ], "TestAuth"));

        return new AccountController(
            signInManager.Object,
            userManager.Object,
            Options.Create(new AdminBootstrapSettings { Password = bootstrapPassword }),
            new FakeHostEnvironment(isDevelopment),
            CreateDevelopmentResetService(),
            analyticsService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            Url = CreateUrlHelper(isLocalUrl: true)
        };
    }

    private static DevelopmentDatabaseResetService CreateDevelopmentResetService()
    {
        return new DevelopmentDatabaseResetService(
            Options.Create(new MongoDbSettings()),
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManager()
    {
        var userManager = CreateUserManager().Object;
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var principalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

        return new Mock<SignInManager<ApplicationUser>>(
            userManager,
            contextAccessor.Object,
            principalFactory.Object,
            null!,
            null!,
            null!,
            null!);
    }

    private static IUrlHelper CreateUrlHelper(bool isLocalUrl)
    {
        var helper = new Mock<IUrlHelper>();
        helper.Setup(instance => instance.IsLocalUrl(It.IsAny<string?>())).Returns(isLocalUrl);
        return helper.Object;
    }

    private sealed class FakeHostEnvironment(bool isDevelopment) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = isDevelopment ? Environments.Development : Environments.Production;

        public string ApplicationName { get; set; } = "AxlProtocolMusic.WebApp";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeAnalyticsService : IAnalyticsService
    {
        public List<string> DeletedVisitorIds { get; } = [];

        public Task RecordPageVisitAsync(PageVisitMetric metric, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task RecordExternalLinkClickAsync(ExternalLinkClickMetric metric, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteVisitorDataAsync(string clientId, CancellationToken cancellationToken = default)
        {
            DeletedVisitorIds.Add(clientId);
            return Task.CompletedTask;
        }

        public Task DeleteVisitorLocationDataAsync(string clientId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AnalyticsDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<VisitorCollectedDataViewModel> GetVisitorCollectedDataAsync(string clientId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
