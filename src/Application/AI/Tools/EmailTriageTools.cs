using Application.Features.Workspace.EmailAccounts;
using Infrastructure.Mailbox;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace Application.AI.Tools;

public sealed class EmailTriageTools(MailboxAgentService agentService)
{
    #region # Public

    public IList<AITool> CreateTools(Guid userId, Guid threadId, string? defaultMailboxAlias = null)
    {
        _ = threadId;
        return new Session(agentService, userId, NullIfWhiteSpace(defaultMailboxAlias)).CreateTools();
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    #endregion

    private sealed class Session(MailboxAgentService agentService, Guid userId, string? defaultMailboxAlias)
    {
        private const string MailboxAliasHint =
            "Connected mailbox alias or mailbox:alias; empty uses @mailbox mention from this turn, else default account.";

        private InboxListSnapshot? _lastList;

        #region # Tool catalog

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
                        [Description("1-based list row (#1, #2, …). Use 0 when using uid. Uses the most recent list_inbox_messages in this turn.")] int listIndex,
                        [Description("IMAP folder: empty/inbox (default). Override only when different from the list call.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        GetInboxMessageAsync(uid, listIndex, folder, mailboxAlias),
                    name: "get_inbox_message",
                    description:
                        "Fetches one message by UID or list row (#N) with full plain-text body and attachment names. " +
                        "After list_inbox_messages, open by list_index without repeating filters. " +
                        "Or pass uid + folder from a list row."),
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

        #endregion

        #region # Queries — List

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
            if (!InboxListRequestBuilder.TryBuild(
                    since, until, limit, countOnly, unreadOnly, fromSender, subjectContains, folder,
                    out var listRequest, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.ListInboxAsync(userId, listRequest!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            if (!listRequest!.CountOnly)
            {
                _lastList = InboxListSnapshot.From(listRequest, result!, mailboxRef);
            }

            var body = listRequest.CountOnly
                ? EmailMailboxTextHelpers.FormatInboxCount(result!.TotalMatched, listRequest.QueryLabel)
                : EmailMailboxTextHelpers.FormatInboxList(result!.Messages, listRequest.QueryLabel, result.TotalMatched);

            return WithAccountHeader(account!, body);
        }

        #endregion

        #region # Queries — Open

        private async Task<string> GetInboxMessageAsync(uint uid, int listIndex, string folder, string mailboxAlias)
        {
            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            if (!InboxOpenRequestBuilder.TryResolve(uid, listIndex, folder, mailboxRef, _lastList, out var openRequest, out var resolveError))
            {
                return resolveError!;
            }

            var (account, message, error) = await agentService.OpenInboxAsync(userId, openRequest!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatInboxMessage(message!));
        }

        private async Task<string> GetInboxMessagesAsync(string uidsCsv, string folder, string mailboxAlias)
        {
            if (!MailboxMessageBatchRequestBuilder.TryBuild(uidsCsv, folder, out var batchRequest, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, messages, error) = await agentService.OpenInboxBatchAsync(userId, batchRequest!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatInboxMessages(messages!));
        }

        #endregion

        #region # Queries — Folders & status

        private async Task<string> ListMailboxFoldersAsync(string mailboxAlias)
        {
            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, folders, error) = await agentService.ListFoldersAsync(userId, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatFolderList(folders!));
        }

        private async Task<string> GetMailboxStatusAsync(string mailboxAlias)
        {
            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, status, error) = await agentService.GetStatusAsync(userId, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, status!.Message);
        }

        #endregion

        #region # Commands

        private async Task<string> DeleteMessagesAsync(string uidsCsv, string folder, string mailboxAlias)
        {
            if (!MailboxMessageBatchRequestBuilder.TryBuild(uidsCsv, folder, out var batchRequest, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.DeleteMessagesAsync(userId, batchRequest!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatCommandResult(result!));
        }

        private async Task<string> MoveMessagesAsync(
            string uidsCsv,
            string folder,
            string destinationFolder,
            string mailboxAlias)
        {
            if (!MailboxMoveRequestBuilder.TryBuild(uidsCsv, folder, destinationFolder, out var moveRequest, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.MoveMessagesAsync(userId, moveRequest!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatCommandResult(result!));
        }

        private async Task<string> SetMessageFlagsAsync(
            string uidsCsv,
            string folder,
            string flagAction,
            string mailboxAlias)
        {
            if (!MailboxFlagRequestBuilder.TryBuild(uidsCsv, folder, flagAction, out var flagRequest, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.SetMessageFlagsAsync(userId, flagRequest!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatCommandResult(result!));
        }

        private async Task<string> SendEmailAsync(string to, string subject, string body, string mailboxAlias)
        {
            if (!SendMailRequestBuilder.TryBuild(to, subject, body, out var sendRequest, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.SendAsync(userId, sendRequest!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, result!.Message);
        }

        #endregion

        #region # Session helpers

        private string? EffectiveMailboxAlias(string? mailboxAlias) =>
            NullIfWhiteSpace(mailboxAlias) ?? defaultMailboxAlias;

        private static string WithAccountHeader(MailboxAccountContext account, string body) =>
            $"{EmailReadConstants.FormatMailboxHeader(account)}\n{body}";

        private static string? NullIfWhiteSpace(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        #endregion
    }
}
