namespace Core.DTOs;

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
