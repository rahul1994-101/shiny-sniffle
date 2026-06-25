using System.Globalization;
using System.Text;

using WebApp.Models;
using WebApp.Utilities.Helpers;

namespace WebApp.Utilities.Helpers;

internal static class EmailMailboxTextHelpers
{
    internal static string FormatInboxQueryLabel(string rangeLabel, InboxQuery query)
    {
        var parts = new List<string>();

        if (!MailboxFolderResolver.IsInboxAlias(query.Folder))
        {
            parts.Add($"folder '{query.Folder!.Trim()}'");
        }

        parts.Add(rangeLabel);

        if (query.UnreadOnly)
        {
            parts.Add("unread only");
        }

        if (!string.IsNullOrWhiteSpace(query.FromContains))
        {
            parts.Add($"from contains '{query.FromContains.Trim()}'");
        }

        if (!string.IsNullOrWhiteSpace(query.SubjectContains))
        {
            parts.Add($"subject contains '{query.SubjectContains.Trim()}'");
        }

        return string.Join(", ", parts);
    }

    internal static string FormatInboxCount(int count, string queryLabel) =>
        count == 0
            ? $"No messages found for {queryLabel}."
            : $"Message count for {queryLabel}: {count}.";

    internal static string FormatInboxList(IReadOnlyList<InboxMessageSummary> messages, string queryLabel, int totalMatched)
    {
        if (messages.Count == 0)
        {
            return totalMatched == 0
                ? $"No messages found for {queryLabel}."
                : $"No messages to show for {queryLabel} ({totalMatched} matched).";
        }

        var shownNote = totalMatched > messages.Count
            ? $"{messages.Count} shown of {totalMatched} matched"
            : $"{messages.Count}";

        var builder = new StringBuilder();
        builder.AppendLine(
            $"Messages for {queryLabel} ({shownNote}; previews up to {EmailReadConstants.SnippetMaxLength} chars — use get_inbox_message with folder + Uid for full body):");
        for (var i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            builder.Append('#').Append(i + 1)
                .Append(" | Uid: ").Append(message.Uid)
                .Append(" | From: ")
                .Append(message.From)
                .Append(" | Subject: ")
                .Append(message.Subject)
                .Append(" | Date: ")
                .Append(message.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(message.Snippet))
            {
                builder.Append(" | Preview: ").Append(message.Snippet);
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    internal static string FormatInboxMessage(InboxMessageDetail message)
    {
        var builder = new StringBuilder();
        builder.Append("Message (folder: ").Append(message.Folder)
            .Append(", Uid: ").Append(message.Uid).AppendLine("):");
        builder.Append("From: ").AppendLine(message.From);
        builder.Append("Subject: ").AppendLine(message.Subject);
        builder.Append("Date: ").AppendLine(message.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

        if (message.AttachmentNames.Count == 0)
        {
            builder.AppendLine("Attachments: none");
        }
        else
        {
            builder.Append("Attachments: ").AppendLine(string.Join(", ", message.AttachmentNames));
        }

        builder.AppendLine();
        builder.Append(message.BodyFromHtml ? "Body (converted from HTML):" : "Body:");
        builder.AppendLine();
        builder.Append(message.Body);
        return builder.ToString().TrimEnd();
    }

    internal static string FormatFolderList(IReadOnlyList<MailboxFolderInfo> folders)
    {
        if (folders.Count == 0)
        {
            return "No mailbox folders were found.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Mailbox folders ({folders.Count}). Use folder name or role alias (inbox, sent, drafts, trash, junk) on list/get tools:");
        foreach (var folder in folders)
        {
            builder.Append("- ").Append(folder.Name);
            if (!folder.Name.Equals(folder.FullName, StringComparison.Ordinal))
            {
                builder.Append(" (path: ").Append(folder.FullName).Append(')');
            }

            if (!string.IsNullOrWhiteSpace(folder.Role))
            {
                builder.Append(" [").Append(folder.Role).Append(']');
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}
