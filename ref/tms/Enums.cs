using System.ComponentModel.DataAnnotations;

namespace WebUI.Models;

public enum Roles : byte
{
    [Display(Name = "None")]
    None = 0,
    [Display(Name = "Member")]
    Member = 1,
    [Display(Name = "Admin")]
    Admin = 2
}