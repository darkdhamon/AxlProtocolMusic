using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models.Identity;
using AxlProtocolMusic.WebApp.Services.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AxlProtocolMusic.WebApp.Tests.Services.Identity;

[TestFixture]
public sealed class AdminIdentitySeederTests
{
    [Test]
    public async Task SeedAsync_WhenCredentialsAreMissing_SkipsBootstrapWork()
    {
        var userManager = CreateUserManager();
        var roleManager = CreateRoleManager();
        var seeder = CreateSeeder(
            userManager,
            roleManager,
            new AdminBootstrapSettings { UserName = " ", Email = "admin@example.com", Password = "password", RoleName = "Admin" });

        await seeder.SeedAsync();

        userManager.Verify(instance => instance.FindByNameAsync(It.IsAny<string>()), Times.Never);
        roleManager.Verify(instance => instance.RoleExistsAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task SeedAsync_WhenRoleAndUserDoNotExist_CreatesAndAssignsBootstrapAdmin()
    {
        var userManager = CreateUserManager();
        var roleManager = CreateRoleManager();
        var settings = new AdminBootstrapSettings
        {
            UserName = "admin-user",
            Email = "admin@example.com",
            Password = "secret-password",
            RoleName = "Admin"
        };

        roleManager.Setup(instance => instance.RoleExistsAsync("Admin")).ReturnsAsync(false);
        roleManager.Setup(instance => instance.CreateAsync(It.IsAny<ApplicationRole>())).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(instance => instance.FindByNameAsync("admin-user")).ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(instance => instance.FindByEmailAsync("admin@example.com")).ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(instance => instance.GetUsersInRoleAsync("Admin")).ReturnsAsync([]);
        userManager
            .Setup(instance => instance.CreateAsync(It.IsAny<ApplicationUser>(), "secret-password"))
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(instance => instance.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
            .ReturnsAsync(false);
        userManager
            .Setup(instance => instance.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        var seeder = CreateSeeder(userManager, roleManager, settings);

        await seeder.SeedAsync();

        roleManager.Verify(instance => instance.CreateAsync(It.Is<ApplicationRole>(role =>
            role.Name == "Admin" &&
            role.NormalizedName == "ADMIN" &&
            !string.IsNullOrWhiteSpace(role.Id))), Times.Once);
        userManager.Verify(instance => instance.CreateAsync(
            It.Is<ApplicationUser>(user =>
                user.UserName == "admin-user" &&
                user.NormalizedUserName == "ADMIN-USER" &&
                user.Email == "admin@example.com" &&
                user.NormalizedEmail == "ADMIN@EXAMPLE.COM" &&
                user.EmailConfirmed),
            "secret-password"), Times.Once);
        userManager.Verify(instance => instance.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"), Times.Once);
    }

    [Test]
    public async Task SeedAsync_WhenNoBootstrapUserExistsButAnotherAdminAlreadyExists_DoesNotCreateUser()
    {
        var userManager = CreateUserManager();
        var roleManager = CreateRoleManager();
        var settings = new AdminBootstrapSettings
        {
            UserName = "admin-user",
            Email = "admin@example.com",
            Password = "secret-password",
            RoleName = "Admin"
        };

        roleManager.Setup(instance => instance.RoleExistsAsync("Admin")).ReturnsAsync(true);
        userManager.Setup(instance => instance.FindByNameAsync("admin-user")).ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(instance => instance.FindByEmailAsync("admin@example.com")).ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(instance => instance.GetUsersInRoleAsync("Admin")).ReturnsAsync(
        [
            new ApplicationUser { Id = "existing-admin", UserName = "another-admin" }
        ]);

        var seeder = CreateSeeder(userManager, roleManager, settings);

        await seeder.SeedAsync();

        userManager.Verify(instance => instance.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        userManager.Verify(instance => instance.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ResetBootstrapAdminAsync_WhenUserNameAndEmailResolveToDifferentUsers_DeletesBothThenRecreatesBootstrapAdmin()
    {
        var userManager = CreateUserManager();
        var roleManager = CreateRoleManager();
        var settings = new AdminBootstrapSettings
        {
            UserName = "admin-user",
            Email = "admin@example.com",
            Password = "secret-password",
            RoleName = "Admin"
        };
        var userByName = new ApplicationUser { Id = "user-1", UserName = "admin-user" };
        var userByEmail = new ApplicationUser { Id = "user-2", Email = "admin@example.com" };

        roleManager.Setup(instance => instance.RoleExistsAsync("Admin")).ReturnsAsync(true);
        userManager.Setup(instance => instance.FindByNameAsync("admin-user")).ReturnsAsync(userByName);
        userManager.Setup(instance => instance.FindByEmailAsync("admin@example.com")).ReturnsAsync(userByEmail);
        userManager.Setup(instance => instance.DeleteAsync(userByName)).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(instance => instance.DeleteAsync(userByEmail)).ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(instance => instance.CreateAsync(It.IsAny<ApplicationUser>(), "secret-password"))
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(instance => instance.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        var seeder = CreateSeeder(userManager, roleManager, settings);

        await seeder.ResetBootstrapAdminAsync();

        userManager.Verify(instance => instance.DeleteAsync(userByName), Times.Once);
        userManager.Verify(instance => instance.DeleteAsync(userByEmail), Times.Once);
        userManager.Verify(instance => instance.CreateAsync(It.IsAny<ApplicationUser>(), "secret-password"), Times.Once);
        userManager.Verify(instance => instance.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"), Times.Once);
    }

    private static AdminIdentitySeeder CreateSeeder(
        Mock<UserManager<ApplicationUser>> userManager,
        Mock<RoleManager<ApplicationRole>> roleManager,
        AdminBootstrapSettings settings)
    {
        return new AdminIdentitySeeder(
            userManager.Object,
            roleManager.Object,
            Options.Create(settings),
            new FakeLogger<AdminIdentitySeeder>());
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

    private static Mock<RoleManager<ApplicationRole>> CreateRoleManager()
    {
        var store = new Mock<IRoleStore<ApplicationRole>>();
        return new Mock<RoleManager<ApplicationRole>>(
            store.Object,
            null!,
            null!,
            null!,
            null!);
    }

    private sealed class FakeLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}
