using MailKit;
using MailKit.Net.Imap;
using MimeKit;

namespace Infrastructure.Mailbox;

internal static class MailboxMessageHelpers
{
    private const MessageSummaryItems DetailFlagItems =
        MessageSummaryItems.UniqueId | MessageSummaryItems.Flags;

    internal static async Task<InboxMessageDetail?> GetDetailAsync(
        IMailFolder folder,
        uint uid,
        CancellationToken cancellationToken)
    {
        var results = await GetDetailsAsync(folder, [uid], cancellationToken);
        return results.Count == 0 ? null : results[0];
    }

    internal static async Task<IReadOnlyList<InboxMessageDetail>> GetDetailsAsync(
        IMailFolder folder,
        IReadOnlyList<uint> uids,
        CancellationToken cancellationToken)
    {
        if (uids.Count == 0)
        {
            return [];
        }

        var uniqueIds = uids.Select(uid => new UniqueId(uid)).ToList();
        var summaries = await folder.FetchAsync(uniqueIds, DetailFlagItems, cancellationToken);
        var flagsByUid = summaries.ToDictionary(s => s.UniqueId.Id, s => s.Flags);

        var details = new List<InboxMessageDetail>(uids.Count);
        foreach (var uid in uids)
        {
            if (!flagsByUid.ContainsKey(uid))
            {
                continue;
            }

            var message = await folder.GetMessageAsync(new UniqueId(uid), cancellationToken);
            flagsByUid.TryGetValue(uid, out var flags);
            details.Add(MapDetail(message, folder.FullName, uid, flags));
        }

        return details;
    }

    internal static async Task<IReadOnlyList<InboxMessageDetail>> GetManyAsync(
        ImapClient imap,
        IReadOnlyList<MessageRef> messages,
        CancellationToken cancellationToken)
    {
        var found = new Dictionary<MessageRefKey, InboxMessageDetail>(MessageRefKey.Comparer);

        foreach (var group in MailboxFolderResolverHelpers.GroupMessagesByFolder(messages))
        {
            var folder = await MailboxFolderResolverHelpers.GetFolderAsync(imap, group.Key, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            var uids = group.Select(m => m.Uid).Distinct().ToList();
            var folderDetails = await GetDetailsAsync(folder, uids, cancellationToken);
            var detailsByUid = folderDetails.ToDictionary(d => d.Uid);

            foreach (var message in group)
            {
                if (detailsByUid.TryGetValue(message.Uid, out var detail))
                {
                    found[new MessageRefKey(message)] = detail;
                }
            }
        }

        var ordered = new List<InboxMessageDetail>(messages.Count);
        foreach (var message in messages)
        {
            if (found.TryGetValue(new MessageRefKey(message), out var detail))
            {
                ordered.Add(detail);
            }
        }

        return ordered;
    }

    private static InboxMessageDetail MapDetail(MimeMessage message, string folderName, uint uid, MessageFlags? flags)
    {
        var body = EmailMessageBodyHelpers.GetPlainBody(message);

        return new InboxMessageDetail
        {
            Uid = uid,
            From = message.From?.ToString() ?? "(unknown)",
            Subject = string.IsNullOrWhiteSpace(message.Subject) ? "(no subject)" : message.Subject,
            Date = message.Date,
            Body = body.Text,
            Folder = folderName,
            BodyFromHtml = body.FromHtml,
            IsUnread = flags is null || !flags.Value.HasFlag(MessageFlags.Seen),
            AttachmentNames = EmailMessageBodyHelpers.GetAttachmentNames(message)
        };
    }

    private readonly record struct MessageRefKey(string? Folder, uint Uid)
    {
        internal MessageRefKey(MessageRef message)
            : this(MailboxFolderResolverHelpers.NormalizeFolderKey(message.Folder), message.Uid)
        {
        }

        internal static IEqualityComparer<MessageRefKey> Comparer { get; } =
            EqualityComparer<MessageRefKey>.Create(
                (left, right) =>
                    left.Uid == right.Uid &&
                    string.Equals(left.Folder, right.Folder, StringComparison.OrdinalIgnoreCase),
                key => HashCode.Combine(
                    key.Uid,
                    key.Folder is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(key.Folder)));
    }
}
