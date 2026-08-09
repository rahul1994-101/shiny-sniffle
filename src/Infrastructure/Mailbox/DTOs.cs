namespace Infrastructure.Mailbox;

using Infrastructure.Persistence.Entities;

public sealed class InboxMessageSummary
{
    public uint Uid { get; init; }

    public string From { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset Date { get; init; }

    public string? Snippet { get; init; }
}

public sealed class InboxQuery
{
    public DateTime? SinceUtc { get; init; }

    /// <summary>Exclusive upper bound for IMAP DeliveredBefore; null means no upper bound.</summary>
    public DateTime? UntilUtcExclusive { get; init; }

    public int Limit { get; init; } = 20;

    public bool CountOnly { get; init; }

    public bool UnreadOnly { get; init; }

    public string? FromContains { get; init; }

    public string? SubjectContains { get; init; }

    /// <summary>IMAP folder: empty/inbox, sent, drafts, trash, junk, or exact name from list_mailbox_folders.</summary>
    public string? Folder { get; init; }
}

public sealed class InboxListResult
{
    public IReadOnlyList<InboxMessageSummary> Messages { get; init; } = [];

    public int TotalMatched { get; init; }
}

public sealed class InboxMessageDetail
{
    public uint Uid { get; init; }

    public string From { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset Date { get; init; }

    public string Body { get; init; } = string.Empty;

    public string Folder { get; init; } = "INBOX";

    public bool BodyFromHtml { get; init; }

    public IReadOnlyList<string> AttachmentNames { get; init; } = [];
}

public sealed class MailboxFolderInfo
{
    public string Name { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string? Role { get; init; }
}

public sealed class OutboundMail
{
    public string To { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;
}

public sealed class MailboxStatusResult
{
    public bool IsConfigured { get; init; }

    public bool IsReachable { get; init; }

    public string Message { get; init; } = string.Empty;
}

public sealed class MailboxTestResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool ImapOk { get; init; }

    public bool SmtpOk { get; init; }
}

public sealed class SendMailResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Resolved IMAP/SMTP connection config passed to <see cref="IMailboxService"/>
/// (built in Application from <c>workspace.EmailAccount</c> + <c>dbo.EmailProvider</c>).
/// </summary>
public class EmailSettings
{
    public EmailProviderPreset Provider { get; set; } = EmailProviderPreset.Custom;

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
