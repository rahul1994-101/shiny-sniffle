using System.ComponentModel;

using Microsoft.Extensions.AI;

namespace WebApp.AI.Tools.Email;

/// <summary>
/// Mail capabilities exposed to the email agent. Mock implementations until mailbox integration ships.
/// </summary>
public sealed class EmailTools
{
    public IList<AITool> CreateTools(Guid userId, Guid chatThreadId) =>
    [
        AIFunctionFactory.Create(
            (string since, int limit) => ListInboxMessages(userId, since, limit),
            name: "list_inbox_messages",
            description: "Lists inbox messages for the connected mailbox in a date range."),
        AIFunctionFactory.Create(
            (string to, string subject, string body) => SendEmail(userId, to, subject, body),
            name: "send_email",
            description: "Sends an email via SMTP for the user's connected mailbox."),
        AIFunctionFactory.Create(
            () => GetMailboxStatus(userId),
            name: "get_mailbox_status",
            description: "Returns whether a mailbox is configured and reachable for the user.")
    ];

    private static string ListInboxMessages(
        Guid userId,
        [Description("Start of range: today, yesterday, last_week, or an ISO date.")]
        string since = "today",
        [Description("Maximum number of messages to return.")]
        int limit = 20) =>
        $"""
        [Mock] Inbox listing for user {userId:D} (since={since}, limit={limit})
        - From: alice@example.com | Subject: Weekly report | Date: today 09:12
        - From: bob@example.com | Subject: Meeting notes | Date: today 08:45
        - From: alerts@example.com | Subject: Your subscription | Date: yesterday 17:30
        """;

    private static string SendEmail(
        Guid userId,
        [Description("Recipient email address.")]
        string to,
        [Description("Email subject line.")]
        string subject,
        [Description("Plain-text body.")]
        string body) =>
        $"[Mock] Email queued for user {userId:D} to {to} with subject \"{subject}\" ({body.Length} chars). Not actually sent.";

    private static string GetMailboxStatus(Guid userId) =>
        $"[Mock] Mailbox status for user {userId:D}: not configured (mock mode). Connect mail in Settings when available.";
}
