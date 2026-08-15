namespace Infrastructure.Persistence.Dbo;

/// <summary>
/// Row in <c>dbo.EmailProvider</c> — IMAP/SMTP catalog for Settings → Email providers.
/// System rows (<see cref="IsSystem"/>) are global read-only templates; custom rows are owned by <see cref="UserId"/>.
/// Runtime mail config resolves endpoints via <c>workspace.EmailAccount.EmailProviderId</c>.
/// </summary>
public class EmailProvider : BaseAuditableEntity
{
    /// <summary>Owner for custom templates; null for system templates.</summary>
    public Guid? UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ImapHost { get; set; } = string.Empty;

    public int ImapPort { get; set; } = 993;

    public bool ImapUseSsl { get; set; } = true;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool SmtpUseSsl { get; set; } = true;

    /// <summary>When true, row is a seeded global template (read-only in app).</summary>
    public bool IsSystem { get; set; }
}
