using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Net.Mail;
using WebApp.Utilities.Helpers;
using WebApp.Utilities.Services;

namespace WebApp.AI.Tools;

public sealed class EmailTools(UserMailboxService _mailboxService)
{
    public IList<AITool> CreateTools(Guid userId, Guid chatThreadId)
    {
        _ = chatThreadId;
        var sinceHint = EmailReadDateContext.SinceToolHint();

        return
        [
            AIFunctionFactory.Create(
                ([Description("Start of date range — see tool description. For explicit user ranges use yyyy-MM-dd or today/yesterday/etc.")] string since,
                    [Description("End date yyyy-MM-dd (inclusive). Use with since start when user gives a from/to range; empty if since is already a full range.")] string until,
                    [Description("Max messages to return (1-50, default 20). Ignored when count_only is true.")] int limit,
                    [Description("When true, return only the message count for the range (no message list).")] bool countOnly,
                    [Description("When true, only unread messages. Combines with since and other filters.")] bool unreadOnly,
                    [Description("Filter by sender name or email substring. Empty means no sender filter.")] string fromSender,
                    [Description("Filter by subject keyword substring. Empty means no subject filter.")] string subjectContains,
                    [Description("IMAP folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder) =>
                    ListInboxMessagesAsync(userId, since, until, limit, countOnly, unreadOnly, fromSender, subjectContains, folder),
                name: "list_inbox_messages",
                description:
                    "Lists or counts messages (#N, Uid, from, subject, date, preview) in a mailbox folder. " +
                    $"Since: {sinceHint} " +
                    "Optional filters: unread_only, from_sender, subject_contains (combine with since). " +
                    "Set count_only for how-many questions. Use get_inbox_message with folder + Uid for full body."),
            AIFunctionFactory.Create(
                ([Description("IMAP UID from a list_inbox_messages row. Use 0 when using list_index.")] uint uid,
                    [Description("1-based list row (#1, #2, …). Use 0 when using uid. Requires since and matching list filters.")] int listIndex,
                    [Description("Same since rules as list_inbox_messages. Empty means today.")] string since,
                    [Description("Same until as list_inbox_messages when using list_index on a date range. Empty if not a range.")] string until,
                    [Description("Same limit as list_inbox_messages when using list_index (1-50, default 20).")] int limit,
                    [Description("Same as list_inbox_messages when using list_index.")] bool unreadOnly,
                    [Description("Same as list_inbox_messages when using list_index.")] string fromSender,
                    [Description("Same as list_inbox_messages when using list_index.")] string subjectContains,
                    [Description("Same folder as list_inbox_messages (empty/inbox default). Required when using list_index.")] string folder) =>
                    GetInboxMessageAsync(userId, uid, listIndex, since, until, limit, unreadOnly, fromSender, subjectContains, folder),
                name: "get_inbox_message",
                description:
                    $"Fetches one message by UID or list row with full plain-text body and attachment names. Since: {sinceHint} " +
                    "Use the same folder as the list call. Prefer uid from list_inbox_messages."),
            AIFunctionFactory.Create(
                () => ListMailboxFoldersAsync(userId),
                name: "list_mailbox_folders",
                description:
                    "Lists IMAP folders (name, path, role) for the connected mailbox. " +
                    "Use before custom folder list/read when folder names are unknown."),
            AIFunctionFactory.Create(
                (string to, string subject, string body) => SendEmailAsync(userId, to, subject, body),
                name: "send_email",
                description: "Sends a plain-text email via SMTP for the user's connected mailbox."),
            AIFunctionFactory.Create(
                () => GetMailboxStatusAsync(userId),
                name: "get_mailbox_status",
                description:
                    "Checks whether the mailbox is configured and IMAP is reachable. " +
                    "Use before listing mail when setup or connectivity is uncertain.")
        ];
    }

    #region # Private Helpers

    private async Task<string> ListInboxMessagesAsync(
        Guid userId,
        string since,
        string until,
        int limit,
        bool countOnly,
        bool unreadOnly,
        string fromSender,
        string subjectContains,
        string folder)
    {
        if (!await _mailboxService.IsConfiguredAsync(userId))
        {
            return EmailReadConstants.NotConfiguredForList;
        }

        if (!InboxListRangeParser.TryParse(since, until, out var range) || range is null)
        {
            return EmailReadConstants.FormatSinceParseHint();
        }

        try
        {
            var query = BuildInboxQuery(range, limit, unreadOnly, fromSender, subjectContains, folder, countOnly);
            var result = await _mailboxService.ListInboxAsync(userId, query);
            var queryLabel = EmailMailboxTextHelpers.FormatInboxQueryLabel(range.Label, query);

            return countOnly
                ? EmailMailboxTextHelpers.FormatInboxCount(result.TotalMatched, queryLabel)
                : EmailMailboxTextHelpers.FormatInboxList(result.Messages, queryLabel, result.TotalMatched);
        }
        catch (Exception ex)
        {
            return $"Could not list messages: {ex.Message}";
        }
    }

    private async Task<string> GetInboxMessageAsync(
        Guid userId,
        uint uid,
        int listIndex,
        string since,
        string until,
        int limit,
        bool unreadOnly,
        string fromSender,
        string subjectContains,
        string folder)
    {
        if (!await _mailboxService.IsConfiguredAsync(userId))
        {
            return EmailReadConstants.NotConfiguredForGet;
        }

        var resolvedFolder = NullIfWhiteSpace(folder);
        uint resolvedUid;
        if (uid > 0)
        {
            resolvedUid = uid;
        }
        else if (listIndex >= 1)
        {
            if (!InboxListRangeParser.TryParse(since, until, out var range) || range is null)
            {
                return EmailReadConstants.FormatSinceParseHint();
            }

            try
            {
                var query = BuildInboxQuery(range, limit, unreadOnly, fromSender, subjectContains, folder);
                var result = await _mailboxService.ListInboxAsync(userId, query);
                if (listIndex > result.Messages.Count)
                {
                    return $"List index #{listIndex} is out of range ({result.Messages.Count} message(s) in the current list). List again or use a Uid.";
                }

                resolvedUid = result.Messages[listIndex - 1].Uid;
            }
            catch (Exception ex)
            {
                return $"Could not resolve list index: {ex.Message}";
            }
        }
        else
        {
            return "Provide uid from a list row, or list_index (1-based) with since, folder, and matching list filters.";
        }

        try
        {
            var message = await _mailboxService.GetInboxMessageAsync(userId, resolvedUid, resolvedFolder);
            return message is null
                ? $"No message found with Uid {resolvedUid} in folder '{resolvedFolder ?? "inbox"}'."
                : EmailMailboxTextHelpers.FormatInboxMessage(message);
        }
        catch (Exception ex)
        {
            return $"Could not read message: {ex.Message}";
        }
    }

    private async Task<string> ListMailboxFoldersAsync(Guid userId)
    {
        if (!await _mailboxService.IsConfiguredAsync(userId))
        {
            return EmailReadConstants.NotConfiguredForFolders;
        }

        try
        {
            var folders = await _mailboxService.ListFoldersAsync(userId);
            return EmailMailboxTextHelpers.FormatFolderList(folders);
        }
        catch (Exception ex)
        {
            return $"Could not list mailbox folders: {ex.Message}";
        }
    }

    private static InboxQuery BuildInboxQuery(
        InboxDateRange range,
        int limit,
        bool unreadOnly,
        string fromSender,
        string subjectContains,
        string folder,
        bool countOnly = false) =>
        new()
        {
            SinceUtc = range.SinceUtc,
            UntilUtcExclusive = range.UntilUtcExclusive,
            Limit = EmailReadConstants.ClampListLimit(limit),
            CountOnly = countOnly,
            UnreadOnly = unreadOnly,
            FromContains = NullIfWhiteSpace(fromSender),
            SubjectContains = NullIfWhiteSpace(subjectContains),
            Folder = NullIfWhiteSpace(folder)
        };

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
