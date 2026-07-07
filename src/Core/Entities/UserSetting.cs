namespace Core.Entities;

public class UserSetting : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    /// <summary>JSON payload for <see cref="EmailSettings"/>; column <c>EmailSettingsJson</c>.</summary>
    public string? EmailSettingsJson { get; set; }
}

public class EmailSettings
{
    public EmailProvider Provider { get; set; } = EmailProvider.Custom;

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
