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
    public string EmailAddress { get; init; } = string.Empty;

    public string ImapHost { get; init; } = string.Empty;

    public int ImapPort { get; init; } = 993;

    public bool ImapUseSsl { get; init; } = true;

    public string SmtpHost { get; init; } = string.Empty;

    public int SmtpPort { get; init; } = 587;

    public bool SmtpUseSsl { get; init; } = true;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

#endregion

#region # Shared

/// <summary>Canonical mailbox limits — enforced in <see cref="MailKitMailboxService"/>.</summary>
public static class MailboxLimits
{
    public const int DefaultListLimit = 20;

    public const int MinListLimit = 1;

    public const int MaxListLimit = 50;

    public const int MaxListSkip = 10_000;

    public const int SnippetMaxLength = 120;

    public const int MaxMessageBodyLength = 12_000;

    public const int MaxBatchGetCount = 5;

    public const int MaxBatchCommandCount = 5;

    public const int MaxAttachmentCount = 10;

    public const int MaxAttachmentSizeBytes = 10 * 1024 * 1024;

    public const int MaxOutboundAttachmentCount = 5;

    public const int MaxOutboundAttachmentSizeBytes = 10 * 1024 * 1024;

    public static int ClampListLimit(int limit) =>
        limit <= 0 ? DefaultListLimit : Math.Clamp(limit, MinListLimit, MaxListLimit);

    public static int ClampListSkip(int skip) =>
        skip < 0 ? 0 : Math.Min(skip, MaxListSkip);
}

public sealed class MessageKey
{
    public uint Uid { get; init; }

    /// <summary>IMAP folder: empty/inbox, sent, drafts, trash, junk, or an exact folder name/path.</summary>
    public string? Folder { get; init; }
}

public sealed class MessageBatchFilters
{
    public IReadOnlyList<MessageKey> Messages { get; init; } = [];
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

    /// <summary>Skip this many matches from the newest end before applying <see cref="Limit"/>.</summary>
    public int Skip { get; init; }

    public bool CountOnly { get; init; }

    public bool UnreadOnly { get; init; }

    public string? FromContains { get; init; }

    public string? SubjectContains { get; init; }

    public string? BodyContains { get; init; }

    public string? ToContains { get; init; }

    public bool? HasAttachments { get; init; }

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

    public IReadOnlyList<string> To { get; init; } = [];

    public IReadOnlyList<string> Cc { get; init; } = [];

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset Date { get; init; }

    public string Body { get; init; } = string.Empty;

    public string Folder { get; init; } = "INBOX";

    public bool BodyFromHtml { get; init; }

    public IReadOnlyList<string> AttachmentNames { get; init; } = [];

    public bool IsUnread { get; init; }

    public string? MessageId { get; init; }

    public string? InReplyTo { get; init; }

    public IReadOnlyList<string> References { get; init; } = [];
}

public sealed class GetMessagesResult
{
    public IReadOnlyList<MessageDetail> Messages { get; init; } = [];
}

public sealed class GetAttachmentsFilters
{
    public MessageKey Message { get; init; } = new();

    /// <summary>0-based attachment index from the message. When null, all attachments are returned (up to <see cref="MailboxLimits.MaxAttachmentCount"/>).</summary>
    public int? AttachmentIndex { get; init; }

    /// <summary>Match attachment by file name (case-insensitive). Ignored when <see cref="AttachmentIndex"/> is set.</summary>
    public string? AttachmentName { get; init; }
}

public sealed class AttachmentContent
{
    public int Index { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/octet-stream";

    public byte[] Content { get; init; } = [];
}

public sealed class GetAttachmentsResult
{
    public IReadOnlyList<AttachmentContent> Attachments { get; init; } = [];
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

public sealed class GetFolderFilters
{
    /// <summary>IMAP folder: empty/inbox, sent, drafts, trash, junk, or an exact folder name/path.</summary>
    public string? Folder { get; init; }
}

public sealed class GetFolderResult
{
    public FolderInfo Folder { get; init; } = new();

    public int TotalCount { get; init; }

    public int UnreadCount { get; init; }

    public uint? UidValidity { get; init; }
}

#endregion

#region # Commands

public enum OutboundMailMode
{
    New,
    Reply,
    Forward
}

public sealed class OutboundAttachment
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/octet-stream";

    public byte[] Content { get; init; } = [];
}

public sealed class OutboundMail
{
    /// <summary>Primary recipient(s). Comma-separated addresses are supported.</summary>
    public string To { get; init; } = string.Empty;

    public IReadOnlyList<string> Cc { get; init; } = [];

    public IReadOnlyList<string> Bcc { get; init; } = [];

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string? HtmlBody { get; init; }

    public OutboundMailMode Mode { get; init; } = OutboundMailMode.New;

    /// <summary>Source message for <see cref="OutboundMailMode.Reply"/> or <see cref="OutboundMailMode.Forward"/>.</summary>
    public MessageKey? InReplyTo { get; init; }

    public IReadOnlyList<OutboundAttachment> Attachments { get; init; } = [];
}

public enum MessageFlagAction
{
    Read,
    Unread,
    Flagged,
    Unflagged
}

public sealed class MessageTransferFilters
{
    public IReadOnlyList<MessageKey> Messages { get; init; } = [];

    public string DestinationFolder { get; init; } = string.Empty;
}

public sealed class SetMessageFlagsFilters
{
    public IReadOnlyList<MessageKey> Messages { get; init; } = [];

    public MessageFlagAction Flag { get; init; }
}

public sealed class CreateFolderFilters
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Parent folder path or alias. Empty creates under the personal namespace root.</summary>
    public string? ParentFolder { get; init; }
}

public sealed class SendMailResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;
}

public sealed class SaveDraftResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public uint? Uid { get; init; }

    public string Folder { get; init; } = string.Empty;
}

public sealed class CommandResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public int AffectedCount { get; init; }
}

#endregion
