using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using System.Net.Mail;

namespace Infrastructure.Mailbox;

public sealed class MailKitMailboxService : IMailboxService
{
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

    public async Task<ListMessagesResult> ListMessagesAsync(EmailSettings config, ListMessagesFilters filters, CancellationToken cancellationToken = default)
    {
        var imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);

        try
        {
            var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, filters.Folder, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            return await MailboxQueryHelpers.ListInFolderAsync(folder, filters, cancellationToken);
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
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

        var imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);

        try
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
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
    }

    public async Task<GetAttachmentsResult> GetAttachmentsAsync(EmailSettings config, GetAttachmentsFilters filters, CancellationToken cancellationToken = default)
    {
        if (filters.Message.Uid == 0)
        {
            throw new ArgumentException("Message Uid is required.", nameof(filters));
        }

        var imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);

        try
        {
            var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, filters.Message.Folder, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            return await MailboxQueryHelpers.GetAttachmentsAsync(folder, filters.Message.Uid, filters, cancellationToken);
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
    }

    public async Task<ListFoldersResult> ListFoldersAsync(EmailSettings config, CancellationToken cancellationToken = default)
    {
        var imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);

        try
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
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
    }

    public async Task<GetFolderResult> GetFolderAsync(EmailSettings config, GetFolderFilters filters, CancellationToken cancellationToken = default)
    {
        var imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);

        try
        {
            var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, filters.Folder, cancellationToken);
            return await MailboxQueryHelpers.GetFolderStatsAsync(folder, cancellationToken);
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
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

        var (original, originalError) = await TryResolveOriginalAsync(config, mail, imap: null, cancellationToken);
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

        var imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);

        try
        {
            var (original, originalError) = await TryResolveOriginalAsync(config, mail, imap, cancellationToken);
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
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
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
        var imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);

        try
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
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
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

        var imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);

        try
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
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
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
        var imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);

        try
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
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
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

        var imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);

        try
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
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
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
        var imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);

        try
        {
            var parent = await MailboxFolderResolverHelpers.GetParentFolderAsync(imap, filters.ParentFolder, cancellationToken);
            var created = await parent.CreateAsync(folderName, true, cancellationToken);

            return new CommandResult
            {
                Success = true,
                AffectedCount = 1,
                Message = $"Created folder '{created.FullName}'."
            };
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
    }

    #endregion

    #region # Private Helpers

    private static bool RequiresOriginalMessage(OutboundMail mail) =>
        mail.Mode is OutboundMailMode.Reply or OutboundMailMode.Forward;

    private static async Task<(MimeMessage? Original, string? Error)> TryResolveOriginalAsync(
        EmailSettings config,
        OutboundMail mail,
        ImapClient? imap,
        CancellationToken cancellationToken)
    {
        if (!RequiresOriginalMessage(mail))
        {
            return (null, null);
        }

        if (mail.InReplyTo is null)
        {
            return (null, "InReplyTo message is required for reply and forward.");
        }

        var ownsConnection = imap is null;
        if (ownsConnection)
        {
            imap = await MailboxConnectionHelpers.ConnectImapAsync(config, cancellationToken);
        }

        try
        {
            var original = await MailboxCommandsHelpers.TryFetchOriginalAsync(imap!, mail.InReplyTo, cancellationToken);
            return original is null
                ? (null, "The source message for reply or forward was not found.")
                : (original, null);
        }
        finally
        {
            if (ownsConnection && imap is not null)
            {
                await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
                imap.Dispose();
            }
        }
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
