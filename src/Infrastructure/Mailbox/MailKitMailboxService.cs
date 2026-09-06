using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using System.Collections.Concurrent;
using System.Net.Mail;

namespace Infrastructure.Mailbox;

public sealed class MailKitMailboxService : IMailboxService, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ImapSession> _imapSessions = new(StringComparer.OrdinalIgnoreCase);

    private static readonly CommandResult NoMessagesSpecified = new()
    {
        Success = false,
        Message = "No messages were specified."
    };

    #region # Connection

    public async Task<TestConnectionResult> TestConnectionAsync(EmailSettings config, CancellationToken cancellationToken = default)
    {
        var imap = await MailboxConnectionHelpers.TryImapSessionAsync(config, cancellationToken);
        var smtp = await MailboxConnectionHelpers.TrySmtpSessionAsync(config, cancellationToken);

        return new TestConnectionResult
        {
            ImapOk = imap.Ok,
            SmtpOk = smtp.Ok,
            Message = imap.Ok && smtp.Ok
                ? "IMAP and SMTP are reachable."
                : MailboxConnectionHelpers.FormatConnectionTestMessage(imap.Ok, imap.Error, smtp.Ok, smtp.Error)
        };
    }

    #endregion

    #region # Queries

    public Task<ListMessagesResult> ListMessagesAsync(EmailSettings config, ListMessagesFilters filters, CancellationToken cancellationToken = default)
    {
        return WithImapAsync(config, async imap =>
        {
            var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, filters.Folder, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            return await MailboxQueryHelpers.ListInFolderAsync(folder, filters, cancellationToken);
        }, cancellationToken);
    }

    public async Task<GetMessagesResult> GetMessagesAsync(EmailSettings config, MessageBatchFilters filters, CancellationToken cancellationToken = default)
    {
        var messages = filters.Messages;
        if (messages.Count == 0)
        {
            return new GetMessagesResult();
        }

        if (messages.Count > MailboxLimits.MaxBatchGetCount)
        {
            throw new ArgumentException(
                $"At most {MailboxLimits.MaxBatchGetCount} messages can be read per call.",
                nameof(filters));
        }

        return await WithImapAsync(config, async imap =>
        {
            var found = new Dictionary<MessageLookupKey, MessageDetail>(MessageLookupKey.Comparer);

            foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
            {
                var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
                await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

                var uids = group.Select(m => m.Uid).Distinct().ToList();
                var folderDetails = await MailboxQueryHelpers.GetDetailsAsync(folder, uids, cancellationToken);
                var detailsByUid = folderDetails.ToDictionary(d => d.Uid);

                foreach (var message in group)
                {
                    if (detailsByUid.TryGetValue(message.Uid, out var detail))
                    {
                        found[new MessageLookupKey(message)] = detail;
                    }
                }
            }

            var ordered = new List<MessageDetail>(messages.Count);
            foreach (var message in messages)
            {
                if (found.TryGetValue(new MessageLookupKey(message), out var detail))
                {
                    ordered.Add(detail);
                }
            }

            return new GetMessagesResult { Messages = ordered };
        }, cancellationToken);
    }

    public async Task<GetAttachmentsResult> GetAttachmentsAsync(EmailSettings config, GetAttachmentsFilters filters, CancellationToken cancellationToken = default)
    {
        if (filters.Message.Uid == 0)
        {
            throw new ArgumentException("Message Uid is required.", nameof(filters));
        }

        return await WithImapAsync(config, async imap =>
        {
            var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, filters.Message.Folder, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            return await MailboxQueryHelpers.GetAttachmentsAsync(folder, filters.Message.Uid, filters, cancellationToken);
        }, cancellationToken);
    }

    public Task<ListFoldersResult> ListFoldersAsync(EmailSettings config, CancellationToken cancellationToken = default)
    {
        return WithImapAsync(config, async imap =>
        {
            var folders = new List<FolderInfo>();

            if (imap.Inbox is not null)
            {
                folders.Add(MailboxFolderResolverHelpers.MapFolder(imap.Inbox));
            }

            foreach (var ns in imap.PersonalNamespaces)
            {
                var root = imap.GetFolder(ns);
                await MailboxFolderResolverHelpers.CollectFoldersAsync(root, folders, cancellationToken);
            }

            return new ListFoldersResult
            {
                Folders = folders
                    .GroupBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .OrderBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }, cancellationToken);
    }

    public Task<GetFolderResult> GetFolderAsync(EmailSettings config, GetFolderFilters filters, CancellationToken cancellationToken = default)
    {
        return WithImapAsync(config, async imap =>
        {
            var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, filters.Folder, cancellationToken);
            return await MailboxQueryHelpers.GetFolderStatsAsync(folder, cancellationToken);
        }, cancellationToken);
    }

    #endregion

    #region # Commands

    public async Task<SendMailResult> SendAsync(EmailSettings config, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateOutboundMail(mail, requireRecipients: true);
        if (validationError is not null)
        {
            return new SendMailResult { Success = false, Message = validationError };
        }

        var (original, originalError) = await TryResolveOriginalAsync(config, mail, cancellationToken);
        if (originalError is not null)
        {
            return new SendMailResult { Success = false, Message = originalError };
        }

        var smtp = await MailboxConnectionHelpers.ConnectSmtpAsync(config, cancellationToken);

        try
        {
            var message = MailboxCommandsHelpers.BuildMessage(config, mail, original);
            if (message.To.Count == 0 && message.Cc.Count == 0 && message.Bcc.Count == 0)
            {
                return new SendMailResult
                {
                    Success = false,
                    Message = "At least one recipient is required."
                };
            }

            await smtp.SendAsync(message, cancellationToken);

            var recipientLabel = string.IsNullOrWhiteSpace(mail.To) ? "recipients" : mail.To.Trim();
            return new SendMailResult
            {
                Success = true,
                Message = mail.Mode switch
                {
                    OutboundMailMode.Reply => $"Reply sent to {recipientLabel}.",
                    OutboundMailMode.Forward => $"Message forwarded to {recipientLabel}.",
                    _ => $"Email sent to {recipientLabel}."
                }
            };
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(smtp, cancellationToken);
            smtp.Dispose();
        }
    }

    public async Task<SaveDraftResult> SaveDraftAsync(EmailSettings config, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateOutboundMail(mail, requireRecipients: false);
        if (validationError is not null)
        {
            return new SaveDraftResult { Success = false, Message = validationError };
        }

        return await WithImapAsync(config, async imap =>
        {
            var (original, originalError) = await FetchOriginalOnSessionAsync(imap, mail, cancellationToken);
            if (originalError is not null)
            {
                return new SaveDraftResult { Success = false, Message = originalError };
            }

            var draftsFolder = await MailboxFolderResolverHelpers.GetDraftsFolderAsync(imap, cancellationToken);
            var message = MailboxCommandsHelpers.BuildMessage(config, mail, original);
            var (uid, folderName) = await MailboxCommandsHelpers.AppendDraftAsync(draftsFolder, message, cancellationToken);

            return new SaveDraftResult
            {
                Success = true,
                Uid = uid,
                Folder = folderName,
                Message = uid is null
                    ? $"Draft saved to '{folderName}'."
                    : $"Draft saved to '{folderName}' (Uid {uid})."
            };
        }, cancellationToken);
    }

    public async Task<CommandResult> CopyMessagesAsync(EmailSettings config, MessageTransferFilters filters, CancellationToken cancellationToken = default)
    {
        var messages = filters.Messages;
        if (messages.Count == 0)
        {
            return NoMessagesSpecified;
        }

        if (messages.Count > MailboxLimits.MaxBatchCommandCount)
        {
            throw new ArgumentException(
                $"At most {MailboxLimits.MaxBatchCommandCount} messages can be copied per call.",
                nameof(filters));
        }

        if (string.IsNullOrWhiteSpace(filters.DestinationFolder))
        {
            return new CommandResult
            {
                Success = false,
                Message = "Destination folder is required."
            };
        }

        var destinationName = filters.DestinationFolder.Trim();
        return await WithImapAsync(config, async imap =>
        {
            var destination = await MailboxFolderResolverHelpers.GetFolderAsync(imap, destinationName, cancellationToken);
            var affected = 0;

            foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
            {
                var sourceFolder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
                await sourceFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
                var uids = group.Select(m => new UniqueId(m.Uid)).ToList();
                await sourceFolder.CopyToAsync(uids, destination, cancellationToken);
                affected += uids.Count;
            }

            return new CommandResult
            {
                Success = true,
                AffectedCount = affected,
                Message = affected == 1
                    ? $"Copied 1 message to '{destination.FullName}'."
                    : $"Copied {affected} messages to '{destination.FullName}'."
            };
        }, cancellationToken);
    }

    public async Task<CommandResult> DeleteMessagesAsync(EmailSettings config, MessageBatchFilters filters, CancellationToken cancellationToken = default)
    {
        var messages = filters.Messages;
        if (messages.Count == 0)
        {
            return NoMessagesSpecified;
        }

        if (messages.Count > MailboxLimits.MaxBatchCommandCount)
        {
            throw new ArgumentException(
                $"At most {MailboxLimits.MaxBatchCommandCount} messages can be deleted per call.",
                nameof(filters));
        }

        return await WithImapAsync(config, async imap =>
        {
            var movedToTrash = 0;
            var expunged = 0;

            foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
            {
                var sourceFolder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
                await sourceFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
                var isTrash = MailboxFolderResolverHelpers.IsTrashFolder(sourceFolder);
                var uids = group.Select(m => new UniqueId(m.Uid)).ToList();
                await MailboxCommandsHelpers.DeleteFromFolderAsync(sourceFolder, uids, imap, cancellationToken);

                if (isTrash)
                {
                    expunged += uids.Count;
                }
                else
                {
                    movedToTrash += uids.Count;
                }
            }

            var affected = movedToTrash + expunged;
            return new CommandResult
            {
                Success = true,
                AffectedCount = affected,
                Message = FormatDeleteMessage(movedToTrash, expunged)
            };
        }, cancellationToken);
    }

    public async Task<CommandResult> MoveMessagesAsync(EmailSettings config, MessageTransferFilters filters, CancellationToken cancellationToken = default)
    {
        var messages = filters.Messages;
        if (messages.Count == 0)
        {
            return NoMessagesSpecified;
        }

        if (messages.Count > MailboxLimits.MaxBatchCommandCount)
        {
            throw new ArgumentException(
                $"At most {MailboxLimits.MaxBatchCommandCount} messages can be moved per call.",
                nameof(filters));
        }

        if (string.IsNullOrWhiteSpace(filters.DestinationFolder))
        {
            return new CommandResult
            {
                Success = false,
                Message = "Destination folder is required."
            };
        }

        var destinationName = filters.DestinationFolder.Trim();
        return await WithImapAsync(config, async imap =>
        {
            var destination = await MailboxFolderResolverHelpers.GetFolderAsync(imap, destinationName, cancellationToken);
            var affected = 0;

            foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
            {
                var sourceFolder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
                await sourceFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
                var uids = group.Select(m => new UniqueId(m.Uid)).ToList();
                await sourceFolder.MoveToAsync(uids, destination, cancellationToken);
                affected += uids.Count;
            }

            return new CommandResult
            {
                Success = true,
                AffectedCount = affected,
                Message = affected == 1
                    ? $"Moved 1 message to '{destination.FullName}'."
                    : $"Moved {affected} messages to '{destination.FullName}'."
            };
        }, cancellationToken);
    }

    public async Task<CommandResult> SetMessageFlagsAsync(EmailSettings config, SetMessageFlagsFilters filters, CancellationToken cancellationToken = default)
    {
        var messages = filters.Messages;
        if (messages.Count == 0)
        {
            return NoMessagesSpecified;
        }

        if (messages.Count > MailboxLimits.MaxBatchCommandCount)
        {
            throw new ArgumentException(
                $"At most {MailboxLimits.MaxBatchCommandCount} messages can be updated per call.",
                nameof(filters));
        }

        return await WithImapAsync(config, async imap =>
        {
            var affected = 0;

            foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
            {
                var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
                await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
                var uids = group.Select(m => new UniqueId(m.Uid)).ToList();
                await MailboxCommandsHelpers.ApplyFlagsInFolderAsync(folder, uids, filters.Flag, cancellationToken);
                affected += uids.Count;
            }

            var actionLabel = filters.Flag switch
            {
                MessageFlagAction.Read => "marked read",
                MessageFlagAction.Unread => "marked unread",
                MessageFlagAction.Flagged => "flagged",
                MessageFlagAction.Unflagged => "unflagged",
                _ => "updated"
            };

            return new CommandResult
            {
                Success = true,
                AffectedCount = affected,
                Message = affected == 1
                    ? $"1 message {actionLabel}."
                    : $"{affected} messages {actionLabel}."
            };
        }, cancellationToken);
    }

    public async Task<CommandResult> CreateFolderAsync(EmailSettings config, CreateFolderFilters filters, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filters.Name))
        {
            return new CommandResult
            {
                Success = false,
                Message = "Folder name is required."
            };
        }

        var folderName = filters.Name.Trim();
        return await WithImapAsync(config, async imap =>
        {
            var parent = await MailboxFolderResolverHelpers.GetParentFolderAsync(imap, filters.ParentFolder, cancellationToken);
            var created = await parent.CreateAsync(folderName, true, cancellationToken);

            return new CommandResult
            {
                Success = true,
                AffectedCount = 1,
                Message = $"Created folder '{created.FullName}'."
            };
        }, cancellationToken);
    }

    #endregion

    #region # Private Helpers

    private static bool RequiresOriginalMessage(OutboundMail mail) =>
        mail.Mode is OutboundMailMode.Reply or OutboundMailMode.Forward;

    private Task<(MimeMessage? Original, string? Error)> TryResolveOriginalAsync(
        EmailSettings config,
        OutboundMail mail,
        CancellationToken cancellationToken)
    {
        if (!RequiresOriginalMessage(mail))
        {
            return Task.FromResult<(MimeMessage?, string?)>((null, null));
        }

        return WithImapAsync(config, imap => FetchOriginalOnSessionAsync(imap, mail, cancellationToken), cancellationToken);
    }

    private static async Task<(MimeMessage? Original, string? Error)> FetchOriginalOnSessionAsync(
        ImapClient imap,
        OutboundMail mail,
        CancellationToken cancellationToken)
    {
        if (mail.InReplyTo is null)
        {
            return (null, "InReplyTo message is required for reply and forward.");
        }

        var original = await MailboxCommandsHelpers.TryFetchOriginalAsync(imap, mail.InReplyTo, cancellationToken);
        return original is null
            ? (null, "The source message for reply or forward was not found.")
            : (original, null);
    }

    private async Task<T> WithImapAsync<T>(EmailSettings config, Func<ImapClient, Task<T>> action, CancellationToken cancellationToken)
    {
        var session = _imapSessions.GetOrAdd(ImapSessionKey(config), static _ => new ImapSession());
        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    var imap = await EnsureImapAsync(session, config, cancellationToken);
                    return await action(imap);
                }
                catch (Exception ex) when (attempt == 0 && IsStaleImapSession(ex))
                {
                    await DropImapAsync(session, CancellationToken.None);
                }
            }
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private static async Task<ImapClient> EnsureImapAsync(ImapSession session, EmailSettings config, CancellationToken cancellationToken)
    {
        if (session.Client is { IsConnected: true, IsAuthenticated: true })
        {
            return session.Client;
        }

        await DropImapAsync(session, cancellationToken);
        session.Client = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);
        return session.Client;
    }

    private static async Task DropImapAsync(ImapSession session, CancellationToken cancellationToken)
    {
        var client = session.Client;
        session.Client = null;
        if (client is null)
        {
            return;
        }

        try
        {
            await MailboxConnectionHelpers.DisconnectAsync(client, cancellationToken);
        }
        catch (Exception)
        {
            // Best-effort close of a stale socket.
        }

        client.Dispose();
    }

    private static string ImapSessionKey(EmailSettings config) =>
        $"{config.ImapHost}|{config.ImapPort}|{config.Username}|{config.EmailAddress}";

    private static bool IsStaleImapSession(Exception ex) =>
        ex is ServiceNotConnectedException or ImapProtocolException or IOException or ObjectDisposedException;

    public async ValueTask DisposeAsync()
    {
        foreach (var session in _imapSessions.Values)
        {
            await session.Gate.WaitAsync();
            try
            {
                await DropImapAsync(session, CancellationToken.None);
            }
            finally
            {
                session.Gate.Release();
                session.Gate.Dispose();
            }
        }

        _imapSessions.Clear();
    }

    private sealed class ImapSession
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);

        public ImapClient? Client { get; set; }
    }

    private static string? ValidateOutboundMail(OutboundMail mail, bool requireRecipients)
    {
        if (requireRecipients &&
            string.IsNullOrWhiteSpace(mail.To) &&
            mail.Cc.Count == 0 &&
            mail.Bcc.Count == 0 &&
            mail.Mode == OutboundMailMode.New)
        {
            return "At least one recipient is required.";
        }

        if (!string.IsNullOrWhiteSpace(mail.To))
        {
            foreach (var address in mail.To.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!MailAddress.TryCreate(address, out _))
                {
                    return $"Recipient email address is invalid: '{address}'.";
                }
            }
        }

        foreach (var address in mail.Cc.Concat(mail.Bcc))
        {
            if (!MailAddress.TryCreate(address, out _))
            {
                return $"Email address is invalid: '{address}'.";
            }
        }

        if (mail.Attachments.Count > MailboxLimits.MaxOutboundAttachmentCount)
        {
            return $"At most {MailboxLimits.MaxOutboundAttachmentCount} attachments are allowed per message.";
        }

        foreach (var attachment in mail.Attachments)
        {
            if (attachment.Content.Length > MailboxLimits.MaxOutboundAttachmentSizeBytes)
            {
                return $"Attachment '{attachment.FileName}' exceeds the {MailboxLimits.MaxOutboundAttachmentSizeBytes / (1024 * 1024)} MB limit.";
            }
        }

        return null;
    }

    private static string FormatDeleteMessage(int movedToTrash, int expunged)
    {
        if (movedToTrash > 0 && expunged > 0)
        {
            var moved = movedToTrash == 1
                ? "Moved 1 message to trash"
                : $"Moved {movedToTrash} messages to trash";
            var deleted = expunged == 1
                ? "permanently deleted 1 message"
                : $"permanently deleted {expunged} messages";
            return $"{moved}; {deleted}.";
        }

        if (expunged > 0)
        {
            return expunged == 1
                ? "Permanently deleted 1 message."
                : $"Permanently deleted {expunged} messages.";
        }

        return movedToTrash == 1
            ? "Moved 1 message to trash."
            : $"Moved {movedToTrash} messages to trash.";
    }

    private readonly record struct MessageLookupKey(string? Folder, uint Uid)
    {
        internal MessageLookupKey(MessageKey message)
            : this(MailboxFolderResolverHelpers.NormalizeFolderKey(message.Folder), message.Uid)
        {
        }

        internal static IEqualityComparer<MessageLookupKey> Comparer { get; } =
            EqualityComparer<MessageLookupKey>.Create(
                (left, right) =>
                    left.Uid == right.Uid &&
                    string.Equals(left.Folder, right.Folder, StringComparison.OrdinalIgnoreCase),
                key => HashCode.Combine(
                    key.Uid,
                    key.Folder is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(key.Folder)));
    }

    #endregion
}
