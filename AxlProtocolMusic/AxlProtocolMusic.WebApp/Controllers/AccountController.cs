using AxlProtocolMusic.WebApp.Models.Authentication;
using AxlProtocolMusic.WebApp.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("account")]
public sealed class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
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
}
