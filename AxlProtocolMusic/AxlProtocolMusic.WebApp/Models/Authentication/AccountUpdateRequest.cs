using System.ComponentModel.DataAnnotations;

namespace AxlProtocolMusic.WebApp.Models.Authentication;

public sealed class AccountUpdateRequest
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    public string? ConfirmNewPassword { get; set; }
}
