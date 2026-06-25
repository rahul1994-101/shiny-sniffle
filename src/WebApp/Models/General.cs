using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class SaveGeneralProfileRequest
{
    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Current password is required.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "New password must be between 6 and 255 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
