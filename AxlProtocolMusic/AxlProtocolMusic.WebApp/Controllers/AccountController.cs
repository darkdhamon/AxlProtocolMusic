using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models.Authentication;
using AxlProtocolMusic.WebApp.Models.Identity;
using AxlProtocolMusic.WebApp.Services.Development;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AxlProtocolMusic.WebApp.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("account")]
public sealed class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdminBootstrapSettings _adminBootstrapSettings;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly DevelopmentDatabaseResetService _developmentDatabaseResetService;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IOptions<AdminBootstrapSettings> adminBootstrapOptions,
        IHostEnvironment hostEnvironment,
        DevelopmentDatabaseResetService developmentDatabaseResetService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _adminBootstrapSettings = adminBootstrapOptions.Value;
        _hostEnvironment = hostEnvironment;
        _developmentDatabaseResetService = developmentDatabaseResetService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToLogin("Enter both a username or email and password.", request.ReturnUrl);
        }

        var user = await ResolveUserAsync(request.UserNameOrEmail);
        if (user is null || string.IsNullOrWhiteSpace(user.UserName))
        {
            return RedirectToLogin("Invalid login attempt.", request.ReturnUrl);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (await IsUsingDefaultPasswordAsync(user))
            {
                return LocalRedirect("/account/edit?forcePasswordChange=true");
            }

            return LocalRedirect(GetSafeReturnUrl(request.ReturnUrl));
        }

        var errorMessage = result.IsLockedOut
            ? "This account is locked. Try again later."
            : "Invalid login attempt.";

        return RedirectToLogin(errorMessage, request.ReturnUrl);
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return LocalRedirect("/");
    }

    [AllowAnonymous]
    [HttpPost("reset-development-database")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDevelopmentDatabase()
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            return NotFound();
        }

        await _developmentDatabaseResetService.ResetAsync();
        await _signInManager.SignOutAsync();

        return Redirect("/login?success=Development%20database%20reset.%20Sign%20in%20with%20the%20seeded%20admin%20account.");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] AccountUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAccountEdit("Complete all required account fields.");
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword)
            && !string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            return RedirectToAccountEdit("New password and confirmation do not match.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return LocalRedirect("/login?error=Your session expired. Please log in again.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!passwordValid)
        {
            return RedirectToAccountEdit("Current password is incorrect.");
        }

        user.UserName = request.UserName.Trim();
        user.Email = request.Email.Trim();

        var usernameResult = await _userManager.SetUserNameAsync(user, user.UserName);
        if (!usernameResult.Succeeded)
        {
            return RedirectToAccountEdit(GetFirstError(usernameResult, "Unable to update username."));
        }

        var emailResult = await _userManager.SetEmailAsync(user, user.Email);
        if (!emailResult.Succeeded)
        {
            return RedirectToAccountEdit(GetFirstError(emailResult, "Unable to update email."));
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var passwordResult = await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);

            if (!passwordResult.Succeeded)
            {
                return RedirectToAccountEdit(GetFirstError(passwordResult, "Unable to update password."));
            }
        }

        await _signInManager.RefreshSignInAsync(user);

        return Redirect("/account/edit?success=Account%20details%20updated.");
    }

    private async Task<ApplicationUser?> ResolveUserAsync(string userNameOrEmail)
    {
        return userNameOrEmail.Contains('@')
            ? await _userManager.FindByEmailAsync(userNameOrEmail)
            : await _userManager.FindByNameAsync(userNameOrEmail);
    }

    private IActionResult RedirectToLogin(string errorMessage, string? returnUrl)
    {
        return Redirect(
            $"/login?error={Uri.EscapeDataString(errorMessage)}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/admin")}");
    }

    private string GetSafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/admin";
    }

    private async Task<bool> IsUsingDefaultPasswordAsync(ApplicationUser user)
    {
        return !string.IsNullOrWhiteSpace(_adminBootstrapSettings.Password)
            && await _userManager.CheckPasswordAsync(user, _adminBootstrapSettings.Password);
    }

    private RedirectResult RedirectToAccountEdit(string errorMessage)
    {
        return Redirect($"/account/edit?error={Uri.EscapeDataString(errorMessage)}");
    }

    private static string GetFirstError(IdentityResult result, string fallbackMessage)
    {
        return result.Errors.FirstOrDefault()?.Description ?? fallbackMessage;
    }
}
