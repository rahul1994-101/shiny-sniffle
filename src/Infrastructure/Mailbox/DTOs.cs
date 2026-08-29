namespace Infrastructure.Mailbox;

#region # Connection

public sealed class TestConnectionResult
{
    public bool ImapOk { get; init; }

    public bool SmtpOk { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool AllProtocolsOk => ImapOk && SmtpOk;
}

/// <summary>Resolved IMAP/SMTP connection config passed to <see cref="IMailboxService"/>.</summary>
public class EmailSettings
{
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

#endregion

#region # Queries

public sealed class MessageSummary
{
    public uint Uid { get; init; }

    public string From { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset Date { get; init; }

    public string? Snippet { get; init; }

    public bool IsUnread { get; init; }
}

public sealed class ListMessagesFilters
{
    public DateTime? SinceUtc { get; init; }

    /// <summary>Exclusive upper bound for IMAP DeliveredBefore; null means no upper bound.</summary>
    public DateTime? UntilUtcExclusive { get; init; }

    public int Limit { get; init; } = 20;

    public bool CountOnly { get; init; }

    public bool UnreadOnly { get; init; }

    public string? FromContains { get; init; }

    public string? SubjectContains { get; init; }

    /// <summary>IMAP folder: empty/inbox, sent, drafts, trash, junk, or an exact folder name/path.</summary>
    public string? Folder { get; init; }
}

public sealed class ListMessagesResult
{
    public IReadOnlyList<MessageSummary> Messages { get; init; } = [];

    public int TotalMatched { get; init; }
}


public sealed class MessageDetail
{
    public uint Uid { get; init; }

    public string From { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset Date { get; init; }

    public string Body { get; init; } = string.Empty;

    public string Folder { get; init; } = "INBOX";

    public bool BodyFromHtml { get; init; }

    public IReadOnlyList<string> AttachmentNames { get; init; } = [];

    public bool IsUnread { get; init; }
}

public sealed class GetMessagesFilters
{
    public IReadOnlyList<MessageKey> Messages { get; init; } = [];
}

public sealed class GetMessagesResult
{
    public IReadOnlyList<MessageDetail> Messages { get; init; } = [];
}


public sealed class FolderInfo
{
    public string Name { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string? Role { get; init; }
}

public sealed class ListFoldersResult
{
    public IReadOnlyList<FolderInfo> Folders { get; init; } = [];
}

public sealed class MessageKey
{
    public uint Uid { get; init; }

    /// <summary>IMAP folder: empty/inbox, sent, drafts, trash, junk, or an exact folder name/path.</summary>
    public string? Folder { get; init; }
}

#endregion

#region # Commands

public sealed class OutboundMail
{
    public string To { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;
}

public enum MessageFlagAction
{
    Read,
    Unread,
    Flagged,
    Unflagged
}

public sealed class SendMailResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;
}

public sealed class MailboxCommandResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public int AffectedCount { get; init; }
}

#endregion
