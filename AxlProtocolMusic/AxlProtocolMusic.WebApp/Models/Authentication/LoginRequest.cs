using System.ComponentModel.DataAnnotations;

namespace AxlProtocolMusic.WebApp.Models.Authentication;

public sealed class LoginRequest
{
    [Required]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
