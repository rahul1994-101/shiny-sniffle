using Microsoft.Extensions.AI;
using System.ComponentModel;

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
                (string since, int limit) => _mailboxService.ListInboxMessagesTextAsync(userId, since, limit),
                name: "list_inbox_messages",
                description: "Lists inbox messages for the connected mailbox in a date range."),
            AIFunctionFactory.Create(
                (string to, string subject, string body) => _mailboxService.SendEmailTextAsync(userId, to, subject, body),
                name: "send_email",
                description: "Sends an email via SMTP for the user's connected mailbox."),
            AIFunctionFactory.Create(
                () => _mailboxService.GetMailboxStatusTextAsync(userId),
                name: "get_mailbox_status",
                description: "Returns whether a mailbox is configured and reachable for the user.")
        ];
    }
}
