using System.Globalization;
using System.Text;

using WebApp.Models;

namespace WebApp.Utilities.Helpers;

internal static class EmailMailboxTextHelpers
{
    internal const string NotConfiguredForAgent =
        "Mailbox is not configured. Open Settings and connect IMAP/SMTP for the Email agent.";

    internal const string NotConfiguredForList =
        "Mailbox is not configured. Open Settings and connect IMAP/SMTP before listing mail.";

    internal const string NotConfiguredForSend =
        "Mailbox is not configured. Open Settings and connect IMAP/SMTP before sending mail.";

    internal static DateTime? ParseSinceUtc(string since)
    {
        if (string.IsNullOrWhiteSpace(since))
        {
            return DateTime.UtcNow.Date;
        }

        var value = since.Trim().ToLowerInvariant();
        var today = DateTime.UtcNow.Date;

        return value switch
        {
            "today" => today,
            "yesterday" => today.AddDays(-1),
            "last_week" or "last week" => today.AddDays(-7),
            _ => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed.ToUniversalTime().Date
                : null
        };
    }

    internal static string FormatInboxList(IReadOnlyList<InboxMessageSummary> messages, string since)
    {
        if (messages.Count == 0)
        {
            return $"No inbox messages found since {since}.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Inbox messages since {since} ({messages.Count}):");
        foreach (var message in messages)
        {
            builder.Append("- From: ")
                .Append(message.From)
                .Append(" | Subject: ")
                .Append(message.Subject)
                .Append(" | Date: ")
                .Append(message.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(message.Snippet))
            {
                builder.Append(" | ").Append(message.Snippet);
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}
