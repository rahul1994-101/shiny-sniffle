namespace Infrastructure.Persistence.dbo;

/// <summary>
/// Row in <c>dbo.EmailProvider</c> — IMAP/SMTP catalog for Settings → Email providers.
/// Not the same as <see cref="EmailProviderPreset"/> on <see cref="EmailSettings"/> (runtime mail config).
/// </summary>
public class EmailProvider : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe unique key (e.g. gmail, outlook, custom).</summary>
    public string Slug { get; set; } = string.Empty;

    public string ImapHost { get; set; } = string.Empty;

    public int ImapPort { get; set; } = 993;

    public bool ImapUseSsl { get; set; } = true;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool SmtpUseSsl { get; set; } = true;

    public string? SetupHelpUrl { get; set; }

    public int SortOrder { get; set; }

    /// <summary>When true, delete is blocked in app (seeded providers).</summary>
    public bool IsSystem { get; set; }
}
