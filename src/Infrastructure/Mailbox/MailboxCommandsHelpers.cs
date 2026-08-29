using MailKit;
using MailKit.Net.Imap;

namespace Infrastructure.Mailbox;

internal static class MailboxCommandsHelpers
{
    internal static async Task<MailboxCommandResult> DeleteAsync(
        ImapClient imap,
        IReadOnlyList<MessageRef> messages,
        CancellationToken cancellationToken)
    {
        var affected = 0;

        foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
        {
            var sourceFolder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
            await sourceFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var uids = group.Select(m => new UniqueId(m.Uid)).ToList();
            if (uids.Count == 0)
            {
                continue;
            }

            if (MailboxFolderResolverHelpers.IsTrashFolder(sourceFolder))
            {
                await sourceFolder.AddFlagsAsync(uids, MessageFlags.Deleted, silent: true, cancellationToken);
                await sourceFolder.ExpungeAsync(uids, cancellationToken);
            }
            else
            {
                var trash = await MailboxFolderResolverHelpers.GetTrashFolderAsync(imap, cancellationToken);
                await sourceFolder.MoveToAsync(uids, trash, cancellationToken);
            }

            affected += uids.Count;
        }

        return new MailboxCommandResult
        {
            Success = true,
            AffectedCount = affected,
            Message = affected == 1
                ? "Moved 1 message to trash."
                : $"Moved {affected} messages to trash."
        };
    }

    internal static async Task<MailboxCommandResult> MoveAsync(
        ImapClient imap,
        IReadOnlyList<MessageRef> messages,
        string destinationFolder,
        CancellationToken cancellationToken)
    {
        var destination = await MailboxFolderResolverHelpers.GetFolderAsync(imap, destinationFolder, cancellationToken);
        var affected = 0;

        foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
        {
            var sourceFolder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
            await sourceFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var uids = group.Select(m => new UniqueId(m.Uid)).ToList();
            if (uids.Count == 0)
            {
                continue;
            }

            await sourceFolder.MoveToAsync(uids, destination, cancellationToken);
            affected += uids.Count;
        }

        return new MailboxCommandResult
        {
            Success = true,
            AffectedCount = affected,
            Message = affected == 1
                ? $"Moved 1 message to '{destination.FullName}'."
                : $"Moved {affected} messages to '{destination.FullName}'."
        };
    }

    internal static async Task<MailboxCommandResult> SetFlagsAsync(
        ImapClient imap,
        IReadOnlyList<MessageRef> messages,
        MessageFlagAction flag,
        CancellationToken cancellationToken)
    {
        var affected = 0;

        foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
        {
            var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var uids = group.Select(m => new UniqueId(m.Uid)).ToList();
            if (uids.Count == 0)
            {
                continue;
            }

            switch (flag)
            {
                case MessageFlagAction.Read:
                    await folder.AddFlagsAsync(uids, MessageFlags.Seen, silent: true, cancellationToken);
                    break;
                case MessageFlagAction.Unread:
                    await folder.RemoveFlagsAsync(uids, MessageFlags.Seen, silent: true, cancellationToken);
                    break;
                case MessageFlagAction.Flagged:
                    await folder.AddFlagsAsync(uids, MessageFlags.Flagged, silent: true, cancellationToken);
                    break;
                case MessageFlagAction.Unflagged:
                    await folder.RemoveFlagsAsync(uids, MessageFlags.Flagged, silent: true, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, "Unsupported message flag action.");
            }

            affected += uids.Count;
        }

        var actionLabel = flag switch
        {
            MessageFlagAction.Read => "marked read",
            MessageFlagAction.Unread => "marked unread",
            MessageFlagAction.Flagged => "flagged",
            MessageFlagAction.Unflagged => "unflagged",
            _ => "updated"
        };

        return new MailboxCommandResult
        {
            Success = true,
            AffectedCount = affected,
            Message = affected == 1
                ? $"1 message {actionLabel}."
                : $"{affected} messages {actionLabel}."
        };
    }
}
