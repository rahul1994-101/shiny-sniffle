using Application.AI.Tools;
using Application.Features.Chat.ChatThreads;

namespace Application.AI.Memory;

/// <summary>Persists the last mailbox list per Email thread + mailbox alias so #N and sender hints work across turns.</summary>
public sealed class EmailThreadMemoryService(ChatThreadRepository chatThreadRepo)
{
    internal async Task<IReadOnlyList<MailboxListSnapshot>> GetLastListsAsync(Guid userId, Guid threadId, CancellationToken cancellationToken = default)
    {
        var jsons = await chatThreadRepo.GetEmailListSnapshotJsonsAsync(userId, threadId, cancellationToken);
        var lists = new List<MailboxListSnapshot>(jsons.Count);
        foreach (var json in jsons)
        {
            var snapshot = MailboxListSnapshot.TryParseMemory(json);
            if (snapshot is { Rows.Count: > 0 })
            {
                lists.Add(snapshot);
            }
        }

        return lists;
    }

    internal Task SaveLastListAsync(Guid userId, Guid threadId, MailboxListSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var alias = snapshot.MailboxAlias?.Trim();
        if (string.IsNullOrWhiteSpace(alias))
        {
            return Task.CompletedTask;
        }

        return chatThreadRepo.UpsertEmailListSnapshotAsync(userId, threadId, alias, snapshot.ToMemoryJson(), cancellationToken);
    }

    internal static string FormatContextBlock(IReadOnlyList<MailboxListSnapshot> snapshots) =>
        EmailMailboxTextHelpers.FormatLastListsMemory(snapshots);
}
