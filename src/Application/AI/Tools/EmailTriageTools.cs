using Application.Features.Shared;
using Application.Features.Workspace.EmailAccounts;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Net.Mail;
using Infrastructure.Mailbox;

namespace Application.AI.Tools;

public sealed class EmailTriageTools(UserMailboxService mailboxService)
{
    public IList<AITool> CreateTools(Guid userId, Guid threadId, string? defaultMailboxAlias = null)
    {
        _ = threadId;
        return new Session(mailboxService, userId, NullIfWhiteSpace(defaultMailboxAlias)).CreateTools();
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class Session(UserMailboxService mailboxService, Guid userId, string? defaultMailboxAlias)
    {
        private const string MailboxAliasHint =
            "Connected mailbox alias or mailbox:alias; empty uses @mailbox mention from this turn, else default account.";

        public IList<AITool> CreateTools()
        {
            var sinceHint = EmailReadDateContext.SinceToolHint();
            var limitHint =
                $"{MailboxReadLimits.MinListLimit}-{MailboxReadLimits.MaxListLimit}, default {MailboxReadLimits.DefaultListLimit}";

            return
            [
                AIFunctionFactory.Create(
                    ([Description("Start of date range — see tool description. For explicit user ranges use yyyy-MM-dd or today/yesterday/etc.")] string since,
                        [Description("End date yyyy-MM-dd (inclusive). Use with since start when user gives a from/to range; empty if since is already a full range.")] string until,
                        [Description("Max messages to return; see tool description for allowed range. Ignored when count_only is true.")] int limit,
                        [Description("When true, return only the message count for the range (no message list).")] bool countOnly,
                        [Description("When true, only unread messages. Combines with since and other filters.")] bool unreadOnly,
                        [Description("Filter by sender name or email substring. Empty means no sender filter.")] string fromSender,
                        [Description("Filter by subject keyword substring. Empty means no subject filter.")] string subjectContains,
                        [Description("IMAP folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        ListInboxMessagesAsync(since, until, limit, countOnly, unreadOnly, fromSender, subjectContains, folder, mailboxAlias),
                    name: "list_inbox_messages",
                    description:
                        "Lists or counts messages (#N, Uid, from, subject, date, preview) in a mailbox folder. " +
                        $"Since: {sinceHint} " +
                        $"Limit: {limitHint}. " +
                        "Optional filters: unread_only, from_sender, subject_contains (combine with since). " +
                        "Set count_only for how-many questions. Use mailbox_alias when the user names a specific connected account."),
                AIFunctionFactory.Create(
                    ([Description("IMAP UID from a list_inbox_messages row. Use 0 when using list_index.")] uint uid,
                        [Description("1-based list row (#1, #2, …). Use 0 when using uid. Requires since and matching list filters.")] int listIndex,
                        [Description("Same since rules as list_inbox_messages. Empty means today.")] string since,
                        [Description("Same until as list_inbox_messages when using list_index on a date range. Empty if not a range.")] string until,
                        [Description("Same limit as list_inbox_messages when using list_index; see tool description for allowed range.")] int limit,
                        [Description("Same as list_inbox_messages when using list_index.")] bool unreadOnly,
                        [Description("Same as list_inbox_messages when using list_index.")] string fromSender,
                        [Description("Same as list_inbox_messages when using list_index.")] string subjectContains,
                        [Description("Same folder as list_inbox_messages (empty/inbox default). Required when using list_index.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        GetInboxMessageAsync(uid, listIndex, since, until, limit, unreadOnly, fromSender, subjectContains, folder, mailboxAlias),
                    name: "get_inbox_message",
                    description:
                        $"Fetches one message by UID or list row with full plain-text body and attachment names. Since: {sinceHint} " +
                        $"List limit when using list_index: {limitHint}. " +
                        "Use the same folder and mailbox_alias as the list call."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43). Max 5 per call.")] string uids,
                        [Description("IMAP folder for all Uids: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        GetInboxMessagesAsync(uids, folder, mailboxAlias),
                    name: "get_inbox_messages",
                    description:
                        $"Fetches up to {MailboxReadLimits.MaxBatchGetCount} messages by Uid in one folder with full bodies and attachment names. " +
                        "Use the same mailbox_alias as the list call."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43).")] string uids,
                        [Description("Source folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        DeleteMessagesAsync(uids, folder, mailboxAlias),
                    name: "delete_messages",
                    description:
                        "Moves messages to trash (recoverable). Confirm with the user before deleting. " +
                        "Use folder + Uids from a recent list_inbox_messages call with the same mailbox_alias."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43).")] string uids,
                        [Description("Source folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description("Destination folder: sent, drafts, trash, junk, archive, or name from list_mailbox_folders.")] string destinationFolder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        MoveMessagesAsync(uids, folder, destinationFolder, mailboxAlias),
                    name: "move_messages",
                    description:
                        "Moves messages to another folder (archive, junk, custom folder). Confirm destination with the user when unclear."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43).")] string uids,
                        [Description("Folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description("Flag action: read, unread, flagged, or unflagged.")] string flagAction,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        SetMessageFlagsAsync(uids, folder, flagAction, mailboxAlias),
                    name: "set_message_flags",
                    description:
                        "Updates message flags: read, unread, flagged, or unflagged."),
                AIFunctionFactory.Create(
                    ([Description(MailboxAliasHint)] string mailboxAlias) => ListMailboxFoldersAsync(mailboxAlias),
                    name: "list_mailbox_folders",
                    description:
                        "Lists IMAP folders (name, path, role) for a connected mailbox account."),
                AIFunctionFactory.Create(
                    (string to, string subject, string body, [Description(MailboxAliasHint)] string mailboxAlias) =>
                        SendEmailAsync(to, subject, body, mailboxAlias),
                    name: "send_email",
                    description: "Sends a plain-text email via SMTP from a connected mailbox account."),
                AIFunctionFactory.Create(
                    ([Description(MailboxAliasHint)] string mailboxAlias) => GetMailboxStatusAsync(mailboxAlias),
                    name: "get_mailbox_status",
                    description:
                        "Checks whether a mailbox account is configured and IMAP/SMTP are reachable.")
            ];
        }

        private async Task<(MailboxAccountContext? Account, string? Error)> RequireMailboxAsync(string? mailboxAlias)
        {
            var resolved = await mailboxService.ResolveAccountAsync(userId, EffectiveMailboxAlias(mailboxAlias));
            if (resolved.Context is null)
            {
                return (null, resolved.ErrorMessage ?? EmailReadConstants.NotConfiguredForAgent);
            }

            return (resolved.Context, null);
        }

        private string? EffectiveMailboxAlias(string? mailboxAlias) =>
            NullIfWhiteSpace(mailboxAlias) ?? defaultMailboxAlias;

        private static string WithAccountHeader(MailboxAccountContext account, string body) =>
            $"{EmailReadConstants.FormatMailboxHeader(account)}\n{body}";

        private async Task<string> ListInboxMessagesAsync(
            string since,
            string until,
            int limit,
            bool countOnly,
            bool unreadOnly,
            string fromSender,
            string subjectContains,
            string folder,
            string mailboxAlias)
        {
            var (account, error) = await RequireMailboxAsync(mailboxAlias);
            if (error is not null)
            {
                return error;
            }

            if (!InboxListRangeParser.TryParse(since, until, out var range) || range is null)
            {
                return EmailReadConstants.FormatSinceParseHint();
            }

            try
            {
                var query = BuildInboxQuery(range, limit, unreadOnly, fromSender, subjectContains, folder, countOnly);
                var result = await mailboxService.ListMessagesAsync(userId, query, EffectiveMailboxAlias(mailboxAlias));
                var queryLabel = EmailMailboxTextHelpers.FormatInboxQueryLabel(range.Label, query);

                var body = countOnly
                    ? EmailMailboxTextHelpers.FormatInboxCount(result.TotalMatched, queryLabel)
                    : EmailMailboxTextHelpers.FormatInboxList(result.Messages, queryLabel, result.TotalMatched);

                return WithAccountHeader(account!, body);
            }
            catch (Exception ex)
            {
                return $"Could not list messages: {ex.Message}";
            }
        }

        private async Task<string> GetInboxMessageAsync(
            uint uid,
            int listIndex,
            string since,
            string until,
            int limit,
            bool unreadOnly,
            string fromSender,
            string subjectContains,
            string folder,
            string mailboxAlias)
        {
            var (account, error) = await RequireMailboxAsync(mailboxAlias);
            if (error is not null)
            {
                return error;
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
                    var result = await mailboxService.ListMessagesAsync(userId, query, EffectiveMailboxAlias(mailboxAlias));
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
                var message = await mailboxService.GetMessageAsync(userId, resolvedUid, resolvedFolder, EffectiveMailboxAlias(mailboxAlias));
                if (message is null)
                {
                    return $"No message found with Uid {resolvedUid} in folder '{resolvedFolder ?? "inbox"}'.";
                }

                return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatInboxMessage(message));
            }
            catch (Exception ex)
            {
                return $"Could not read message: {ex.Message}";
            }
        }

        private async Task<string> GetInboxMessagesAsync(string uidsCsv, string folder, string mailboxAlias)
        {
            var (account, error) = await RequireMailboxAsync(mailboxAlias);
            if (error is not null)
            {
                return error;
            }

            if (!TryParseUids(uidsCsv, out var uids, out var parseError))
            {
                return parseError!;
            }

            if (uids.Count > MailboxReadLimits.MaxBatchGetCount)
            {
                return $"At most {MailboxReadLimits.MaxBatchGetCount} Uids per call. Split into multiple get_inbox_messages calls or use get_inbox_message for one message.";
            }

            try
            {
                var refs = BuildMessageRefs(uids, folder);
                var messages = await mailboxService.GetMessagesAsync(userId, refs, EffectiveMailboxAlias(mailboxAlias));
                return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatInboxMessages(messages));
            }
            catch (Exception ex)
            {
                return $"Could not read messages: {ex.Message}";
            }
        }

        private async Task<string> DeleteMessagesAsync(string uidsCsv, string folder, string mailboxAlias)
        {
            var (account, error) = await RequireMailboxAsync(mailboxAlias);
            if (error is not null)
            {
                return error;
            }

            if (!TryParseUids(uidsCsv, out var uids, out var parseError))
            {
                return parseError!;
            }

            try
            {
                var result = await mailboxService.DeleteMessagesAsync(
                    userId,
                    BuildMessageRefs(uids, folder),
                    EffectiveMailboxAlias(mailboxAlias));

                return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatCommandResult(result));
            }
            catch (Exception ex)
            {
                return $"Could not delete messages: {ex.Message}";
            }
        }

        private async Task<string> MoveMessagesAsync(
            string uidsCsv,
            string folder,
            string destinationFolder,
            string mailboxAlias)
        {
            var (account, error) = await RequireMailboxAsync(mailboxAlias);
            if (error is not null)
            {
                return error;
            }

            if (string.IsNullOrWhiteSpace(destinationFolder))
            {
                return "Destination folder is required.";
            }

            if (!TryParseUids(uidsCsv, out var uids, out var parseError))
            {
                return parseError!;
            }

            try
            {
                var result = await mailboxService.MoveMessagesAsync(
                    userId,
                    BuildMessageRefs(uids, folder),
                    destinationFolder,
                    EffectiveMailboxAlias(mailboxAlias));

                return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatCommandResult(result));
            }
            catch (Exception ex)
            {
                return $"Could not move messages: {ex.Message}";
            }
        }

        private async Task<string> SetMessageFlagsAsync(
            string uidsCsv,
            string folder,
            string flagAction,
            string mailboxAlias)
        {
            var (account, error) = await RequireMailboxAsync(mailboxAlias);
            if (error is not null)
            {
                return error;
            }

            if (!TryParseUids(uidsCsv, out var uids, out var parseError))
            {
                return parseError!;
            }

            if (!TryParseFlagAction(flagAction, out var flag, out parseError))
            {
                return parseError!;
            }

            try
            {
                var result = await mailboxService.SetMessageFlagsAsync(
                    userId,
                    BuildMessageRefs(uids, folder),
                    flag,
                    EffectiveMailboxAlias(mailboxAlias));

                return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatCommandResult(result));
            }
            catch (Exception ex)
            {
                return $"Could not update message flags: {ex.Message}";
            }
        }

        private async Task<string> ListMailboxFoldersAsync(string mailboxAlias)
        {
            var (account, error) = await RequireMailboxAsync(mailboxAlias);
            if (error is not null)
            {
                return error;
            }

            try
            {
                var folders = await mailboxService.ListFoldersAsync(userId, EffectiveMailboxAlias(mailboxAlias));
                return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatFolderList(folders));
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
                Limit = MailboxReadLimits.ClampListLimit(limit),
                CountOnly = countOnly,
                UnreadOnly = unreadOnly,
                FromContains = NullIfWhiteSpace(fromSender),
                SubjectContains = NullIfWhiteSpace(subjectContains),
                Folder = NullIfWhiteSpace(folder)
            };

        private static string? NullIfWhiteSpace(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static IReadOnlyList<MessageRef> BuildMessageRefs(IReadOnlyList<uint> uids, string? folder)
        {
            var normalizedFolder = NullIfWhiteSpace(folder);
            return uids.Select(uid => new MessageRef { Uid = uid, Folder = normalizedFolder }).ToList();
        }

        private static bool TryParseUids(string uidsCsv, out List<uint> uids, out string? error)
        {
            uids = [];
            error = null;

            if (string.IsNullOrWhiteSpace(uidsCsv))
            {
                error = "Provide at least one Uid from list_inbox_messages (comma-separated, e.g. 42,43).";
                return false;
            }

            foreach (var part in uidsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!uint.TryParse(part, out var uid) || uid == 0)
                {
                    error = $"Invalid Uid '{part}'. Use numeric Uids from list_inbox_messages.";
                    uids = [];
                    return false;
                }

                uids.Add(uid);
            }

            if (uids.Count == 0)
            {
                error = "Provide at least one Uid from list_inbox_messages (comma-separated, e.g. 42,43).";
                return false;
            }

            return true;
        }

        private static bool TryParseFlagAction(string flagAction, out MessageFlagAction flag, out string? error)
        {
            flag = default;
            error = null;

            if (string.IsNullOrWhiteSpace(flagAction))
            {
                error = "Flag action is required: read, unread, flagged, or unflagged.";
                return false;
            }

            switch (flagAction.Trim().ToLowerInvariant())
            {
                case "read":
                    flag = MessageFlagAction.Read;
                    return true;
                case "unread":
                    flag = MessageFlagAction.Unread;
                    return true;
                case "flagged":
                    flag = MessageFlagAction.Flagged;
                    return true;
                case "unflagged":
                    flag = MessageFlagAction.Unflagged;
                    return true;
                default:
                    error = "Flag action must be read, unread, flagged, or unflagged.";
                    return false;
            }
        }

        private async Task<string> SendEmailAsync(string to, string subject, string body, string mailboxAlias)
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

            var (account, error) = await RequireMailboxAsync(mailboxAlias);
            if (error is not null)
            {
                return error;
            }

            try
            {
                var result = await mailboxService.SendAsync(
                    userId,
                    new OutboundMail { To = to.Trim(), Subject = subject.Trim(), Body = body ?? string.Empty },
                    EffectiveMailboxAlias(mailboxAlias));

                return WithAccountHeader(account!, result.Message);
            }
            catch (Exception ex)
            {
                return $"Could not send email: {ex.Message}";
            }
        }

        private async Task<string> GetMailboxStatusAsync(string mailboxAlias)
        {
            var (account, error) = await RequireMailboxAsync(mailboxAlias);
            if (error is not null)
            {
                return error;
            }

            var status = await mailboxService.GetStatusAsync(userId, EffectiveMailboxAlias(mailboxAlias));
            return WithAccountHeader(account!, status.Message);
        }
    }
}
