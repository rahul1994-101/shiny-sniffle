using MailKit;
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
                : MailboxConnectionHelpers.FormatConnectionProbeMessage(imap.Ok, imap.Error, smtp.Ok, smtp.Error)
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

    #endregion

    #region # Commands

    public async Task<SendMailResult> SendAsync(EmailSettings config, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        if (!MailAddress.TryCreate(mail.To, out _))
        {
            return new SendMailResult
            {
                Success = false,
                Message = "Recipient email address is invalid."
            };
        }

        var smtp = await MailboxConnectionHelpers.ConnectSmtpAsync(config, cancellationToken);

        try
        {
            var message = MailboxCommandsHelpers.BuildMessage(config, mail);
            await smtp.SendAsync(message, cancellationToken);

            return new SendMailResult
            {
                Success = true,
                Message = $"Email sent to {mail.To}."
            };
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(smtp, cancellationToken);
            smtp.Dispose();
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

    public async Task<CommandResult> MoveMessagesAsync(EmailSettings config, MoveMessagesFilters filters, CancellationToken cancellationToken = default)
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

    #endregion

    #region # Private Helpers

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
