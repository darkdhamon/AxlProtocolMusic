using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AxlProtocolMusic.WebApp.Services.Identity;

public sealed class AdminIdentitySeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly AdminBootstrapSettings _settings;
    private readonly ILogger<AdminIdentitySeeder> _logger;

    public AdminIdentitySeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<AdminBootstrapSettings> settings,
        ILogger<AdminIdentitySeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.UserName)
            || string.IsNullOrWhiteSpace(_settings.Email)
            || string.IsNullOrWhiteSpace(_settings.Password))
        {
            _logger.LogInformation("Admin bootstrap skipped because credentials are not configured.");
            return;
        }

        if (!await _roleManager.RoleExistsAsync(_settings.RoleName))
        {
            var roleResult = await _roleManager.CreateAsync(new ApplicationRole
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = _settings.RoleName,
                NormalizedName = _settings.RoleName.ToUpperInvariant()
            });

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create admin role: {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
            }
        }

        var user = await _userManager.FindByNameAsync(_settings.UserName)
            ?? await _userManager.FindByEmailAsync(_settings.Email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                UserName = _settings.UserName,
                NormalizedUserName = _settings.UserName.ToUpperInvariant(),
                Email = _settings.Email,
                NormalizedEmail = _settings.Email.ToUpperInvariant(),
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, _settings.Password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create bootstrap admin user: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await _userManager.IsInRoleAsync(user, _settings.RoleName))
        {
            var addToRoleResult = await _userManager.AddToRoleAsync(user, _settings.RoleName);
            if (!addToRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign admin role: {string.Join(", ", addToRoleResult.Errors.Select(error => error.Description))}");
            }
        }
    }
}
