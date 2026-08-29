using MailKit;

namespace Infrastructure.Mailbox;

internal static class MailboxSummaryHelpers
{
    private const MessageSummaryItems ListFetchItems =
        MessageSummaryItems.UniqueId
        | MessageSummaryItems.Envelope
        | MessageSummaryItems.Flags
        | MessageSummaryItems.PreviewText;

    internal static async Task<InboxListResult> ListAsync(
        IMailFolder folder,
        InboxQuery query,
        CancellationToken cancellationToken)
    {
        var search = MailboxSearchHelpers.BuildQuery(query);
        var ids = await folder.SearchAsync(search, cancellationToken);
        var totalMatched = ids.Count;

        if (query.CountOnly)
        {
            return new InboxListResult { TotalMatched = totalMatched };
        }

        if (ids.Count == 0)
        {
            return new InboxListResult { TotalMatched = 0 };
        }

        var limit = MailboxReadLimits.ClampListLimit(query.Limit);
        var selected = ids.TakeLast(limit).Reverse().ToList();
        var fetched = await folder.FetchAsync(selected, ListFetchItems, cancellationToken);
        var byUid = fetched.ToDictionary(s => s.UniqueId, s => s);

        var summaries = new List<InboxMessageSummary>(selected.Count);
        foreach (var id in selected)
        {
            if (!byUid.TryGetValue(id, out var summary))
            {
                continue;
            }

            summaries.Add(MapSummary(summary));
        }

        return new InboxListResult
        {
            Messages = summaries,
            TotalMatched = totalMatched
        };
    }

    private static InboxMessageSummary MapSummary(IMessageSummary summary)
    {
        var envelope = summary.Envelope;

        return new InboxMessageSummary
        {
            Uid = summary.UniqueId.Id,
            From = envelope?.From?.ToString() ?? "(unknown)",
            Subject = string.IsNullOrWhiteSpace(envelope?.Subject) ? "(no subject)" : envelope.Subject,
            Date = envelope?.Date ?? DateTimeOffset.MinValue,
            IsUnread = IsUnread(summary.Flags),
            Snippet = FormatSnippet(summary.PreviewText)
        };
    }

    private static bool IsUnread(MessageFlags? flags) =>
        flags is null || !flags.Value.HasFlag(MessageFlags.Seen);

    private static string? FormatSnippet(string? preview)
    {
        if (string.IsNullOrWhiteSpace(preview))
        {
            return null;
        }

        var text = preview.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return text.Length <= MailboxReadLimits.SnippetMaxLength
            ? text
            : text[..MailboxReadLimits.SnippetMaxLength] + "…";
    }
}
