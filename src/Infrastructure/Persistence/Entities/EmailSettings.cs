namespace Infrastructure.Persistence.Entities;

/// <summary>
/// Resolved IMAP/SMTP connection config passed to <c>IMailboxService</c> (built from <see cref="EmailAccount"/> + catalog).
/// </summary>
public class EmailSettings
{
    public EmailProvider Provider { get; set; } = EmailProvider.Custom;

    public string ProviderSlug { get; set; } = "custom";

    public string EmailAddress { get; set; } = string.Empty;

    public string ImapHost { get; set; } = string.Empty;

    public int ImapPort { get; set; } = 993;

    public bool ImapUseSsl { get; set; } = true;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool SmtpUseSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
