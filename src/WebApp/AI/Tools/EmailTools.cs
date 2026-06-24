using Microsoft.Extensions.AI;
using System.Net.Mail;

using WebApp.Models;
using WebApp.Utilities.Helpers;
using WebApp.Utilities.Services;

namespace WebApp.AI.Tools;

public sealed class EmailTools(UserMailboxService _mailboxService)
{
    public IList<AITool> CreateTools(Guid userId, Guid chatThreadId)
    {
        _ = chatThreadId;

        return
        [
            AIFunctionFactory.Create(
                (string since, int limit) => ListInboxMessagesAsync(userId, since, limit),
                name: "list_inbox_messages",
                description: "Lists inbox messages for the connected mailbox in a date range."),
            AIFunctionFactory.Create(
                (string to, string subject, string body) => SendEmailAsync(userId, to, subject, body),
                name: "send_email",
                description: "Sends an email via SMTP for the user's connected mailbox."),
            AIFunctionFactory.Create(
                () => GetMailboxStatusAsync(userId),
                name: "get_mailbox_status",
                description: "Returns whether a mailbox is configured and reachable for the user.")
        ];
    }

    #region # Private Helpers

    private async Task<string> ListInboxMessagesAsync(Guid userId, string since, int limit)
    {
        if (!await _mailboxService.IsConfiguredAsync(userId))
        {
            return EmailMailboxTextHelpers.NotConfiguredForList;
        }

        var sinceUtc = EmailMailboxTextHelpers.ParseSinceUtc(since);
        if (sinceUtc is null)
        {
            return "Could not parse the date range. Use today, yesterday, last_week, or an ISO date (yyyy-MM-dd).";
        }

        try
        {
            var messages = await _mailboxService.ListInboxAsync(
                userId,
                new InboxQuery { SinceUtc = sinceUtc, Limit = limit });

            return EmailMailboxTextHelpers.FormatInboxList(messages, since);
        }
        catch (Exception ex)
        {
            return $"Could not list inbox messages: {ex.Message}";
        }
    }

    private async Task<string> SendEmailAsync(Guid userId, string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return "Recipient address is required.";
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return "Subject is required.";
        }

        if (!MailAddress.TryCreate(to, out _))
        {
            return "Recipient email address is invalid.";
        }

        try
        {
            var result = await _mailboxService.SendAsync(
                userId,
                new OutboundMail { To = to.Trim(), Subject = subject.Trim(), Body = body ?? string.Empty });

            return result.Message;
        }
        catch (Exception ex)
        {
            return $"Could not send email: {ex.Message}";
        }
    }

    private async Task<string> GetMailboxStatusAsync(Guid userId)
    {
        var status = await _mailboxService.GetStatusAsync(userId);
        return status.Message;
    }

    #endregion
}
