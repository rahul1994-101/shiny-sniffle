using MailKit;
using System.Net.Mail;

namespace Infrastructure.Mailbox;

public sealed class MailKitMailboxService : IMailboxService
{
    private static readonly MailboxCommandResult NoMessagesSpecified = new()
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
            return await MailboxSummaryHelpers.ListInFolderAsync(folder, filters, cancellationToken);
        }
        finally
        {
            await MailboxConnectionHelpers.DisconnectAsync(imap, cancellationToken);
            imap.Dispose();
        }
    }

    public async Task<GetMessagesResult> GetMessagesAsync(EmailSettings config, GetMessagesFilters filters, CancellationToken cancellationToken = default)
    {
        var messages = filters.Messages;
        if (messages.Count == 0)
        {
            return new GetMessagesResult();
        }

        if (messages.Count > MailboxReadLimits.MaxBatchGetCount)
        {
            throw new ArgumentException(
                $"At most {MailboxReadLimits.MaxBatchGetCount} messages can be read per call.",
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
                var folderDetails = await MailboxMessageHelpers.GetDetailsAsync(folder, uids, cancellationToken);
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

        return await MailboxConnectionHelpers.ExecuteSmtpAsync(config, (smtp, ct) => MailboxSendHelpers.SendAsync(smtp, config, mail, ct), cancellationToken);
    }

    public async Task<MailboxCommandResult> DeleteMessagesAsync(EmailSettings config, IReadOnlyList<MessageKey> messages, CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return NoMessagesSpecified;
        }

        return await MailboxConnectionHelpers.ExecuteImapAsync(config, (imap, ct) => MailboxCommandsHelpers.DeleteAsync(imap, messages, ct), cancellationToken);
    }

    public async Task<MailboxCommandResult> MoveMessagesAsync(EmailSettings config, IReadOnlyList<MessageKey> messages, string destinationFolder, CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return NoMessagesSpecified;
        }

        if (string.IsNullOrWhiteSpace(destinationFolder))
        {
            return new MailboxCommandResult
            {
                Success = false,
                Message = "Destination folder is required."
            };
        }

        var destination = destinationFolder.Trim();
        return await MailboxConnectionHelpers.ExecuteImapAsync(config, (imap, ct) => MailboxCommandsHelpers.MoveAsync(imap, messages, destination, ct), cancellationToken);
    }

    public async Task<MailboxCommandResult> SetMessageFlagsAsync(EmailSettings config, IReadOnlyList<MessageKey> messages, MessageFlagAction flag, CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return NoMessagesSpecified;
        }

        return await MailboxConnectionHelpers.ExecuteImapAsync(config, (imap, ct) => MailboxCommandsHelpers.SetFlagsAsync(imap, messages, flag, ct), cancellationToken);
    }

    #endregion

    #region # Private Helpers

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
