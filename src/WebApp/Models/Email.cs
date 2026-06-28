using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class EmailSettingsDto
{
    public EmailProvider Provider { get; set; } = EmailProvider.Custom;

    [StringLength(255, ErrorMessage = "Email address must be at most 255 characters.")]
    public string EmailAddress { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "IMAP host must be at most 255 characters.")]
    public string ImapHost { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "IMAP port must be between 1 and 65535.")]
    public int ImapPort { get; set; } = 993;

    public bool ImapUseSsl { get; set; } = true;

    [StringLength(255, ErrorMessage = "SMTP host must be at most 255 characters.")]
    public string SmtpHost { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "SMTP port must be between 1 and 65535.")]
    public int SmtpPort { get; set; } = 587;

    public bool SmtpUseSsl { get; set; } = true;

    [StringLength(255, ErrorMessage = "Username must be at most 255 characters.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>Plain password on save only. Never returned on load.</summary>
    [StringLength(255, ErrorMessage = "Password must be at most 255 characters.")]
    public string Password { get; set; } = string.Empty;

    public bool HasStoredPassword { get; set; }
}
