using System.ComponentModel.DataAnnotations;

namespace Core.Entities;

public class User : BaseAuditableEntity
{
    //private string _password;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Email must be between 5 and 255 characters.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20, MinimumLength = 0, ErrorMessage = "Mobile must be between 0 and 20 characters.")]
    public string? Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters.")]
    public string Password { get; set; } = string.Empty;
    //public string Password
    //{
    //    get => _password.Decrypt();
    //    set => _password = value.Encrypt();
    //}
}
