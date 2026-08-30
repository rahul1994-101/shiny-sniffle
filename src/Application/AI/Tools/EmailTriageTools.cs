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

        private MailboxListSnapshot? _lastList;

        #region # Tool catalog

        public IList<AITool> CreateTools()
        {
            var sinceHint = EmailReadDateContext.SinceToolHint();
            var limitHint =
                $"{MailboxLimits.MinListLimit}-{MailboxLimits.MaxListLimit}, default {MailboxLimits.DefaultListLimit}";

            return
            [
                AIFunctionFactory.Create(
                    ([Description("Start of date range — see tool description. For explicit user ranges use yyyy-MM-dd or today/yesterday/etc.")] string since,
                        [Description("End date yyyy-MM-dd (inclusive). Use with since start when user gives a from/to range; empty if since is already a full range.")] string until,
                        [Description("Max messages to return; see tool description for allowed range. Ignored when count_only is true.")] int limit,
                        [Description("Skip this many matches from the newest end before applying limit (pagination). 0 means none.")] int skip,
                        [Description("When true, return only the message count for the range (no message list).")] bool countOnly,
                        [Description("When true, only unread messages. Combines with since and other filters.")] bool unreadOnly,
                        [Description("Filter by sender name or email substring. Empty means no sender filter.")] string fromSender,
                        [Description("Filter by subject keyword substring. Empty means no subject filter.")] string subjectContains,
                        [Description("Filter by body text substring. Empty means no body filter.")] string bodyContains,
                        [Description("Filter by recipient (To) substring. Empty means no to filter.")] string toContains,
                        [Description("Attachment filter: empty (any), yes/with_attachments, or no/without_attachments.")] string attachmentsFilter,
                        [Description("IMAP folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        ListInboxMessagesAsync(since, until, limit, skip, countOnly, unreadOnly, fromSender, subjectContains, bodyContains, toContains, attachmentsFilter, folder, mailboxAlias),
                    name: "list_inbox_messages",
                    description:
                        "Lists or counts messages (#N, Uid, from, subject, date, preview) in a mailbox folder. " +
                        $"Since: {sinceHint} " +
                        $"Limit: {limitHint}. skip for pagination (newest-first). " +
                        "Optional filters: unread_only, from_sender, subject_contains, body_contains, to_contains, attachments_filter. " +
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
                        $"Fetches up to {MailboxLimits.MaxBatchGetCount} messages by Uid in one folder with full bodies and attachment names. " +
                        "Use the same mailbox_alias as the list call."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43). Max 5 per call.")] string uids,
                        [Description("Source folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        DeleteMessagesAsync(uids, folder, mailboxAlias),
                    name: "delete_messages",
                    description:
                        $"Moves up to {MailboxLimits.MaxBatchCommandCount} messages to trash (recoverable). Confirm with the user before deleting. " +
                        "Use folder + Uids from a recent list_inbox_messages call with the same mailbox_alias."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43). Max 5 per call.")] string uids,
                        [Description("Source folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description("Destination folder: sent, drafts, trash, junk, archive, or name from list_mailbox_folders.")] string destinationFolder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        MoveMessagesAsync(uids, folder, destinationFolder, mailboxAlias),
                    name: "move_messages",
                    description:
                        $"Moves up to {MailboxLimits.MaxBatchCommandCount} messages to another folder (archive, junk, custom folder). Confirm destination with the user when unclear."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43). Max 5 per call.")] string uids,
                        [Description("Folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description("Flag action: read, unread, flagged, or unflagged.")] string flagAction,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        SetMessageFlagsAsync(uids, folder, flagAction, mailboxAlias),
                    name: "set_message_flags",
                    description:
                        $"Updates flags on up to {MailboxLimits.MaxBatchCommandCount} messages: read, unread, flagged, or unflagged."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43). Max 5 per call.")] string uids,
                        [Description("Source folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description("Destination folder: sent, drafts, trash, junk, archive, or name from list_mailbox_folders.")] string destinationFolder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        CopyMessagesAsync(uids, folder, destinationFolder, mailboxAlias),
                    name: "copy_messages",
                    description:
                        $"Copies up to {MailboxLimits.MaxBatchCommandCount} messages to another folder without removing them from the source. Confirm destination with the user when unclear."),
                AIFunctionFactory.Create(
                    ([Description("IMAP UID from a list/get call. Use 0 when using list_index.")] uint uid,
                        [Description("1-based list row (#1, #2, …). Use 0 when using uid. Uses the most recent list_inbox_messages in this turn.")] int listIndex,
                        [Description("IMAP folder: empty/inbox (default). Override only when different from the list call.")] string folder,
                        [Description("0-based attachment index from get_inbox_message output. Use -1 when using attachment_name or to fetch all.")] int attachmentIndex,
                        [Description("Attachment file name (case-insensitive). Empty when using attachment_index or fetching all.")] string attachmentName,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        GetAttachmentsAsync(uid, listIndex, folder, attachmentIndex, attachmentName, mailboxAlias),
                    name: "get_attachments",
                    description:
                        "Downloads attachment content for one message. Returns metadata and text preview for small text files; binary/large files show size only. " +
                        "Use uid+folder from a list row, or list_index after list_inbox_messages. Optionally filter by attachment_index or attachment_name."),
                AIFunctionFactory.Create(
                    ([Description("IMAP folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        GetFolderAsync(folder, mailboxAlias),
                    name: "get_folder",
                    description:
                        "Returns folder stats: total message count, unread count, and Uid validity. Use for how-many-unread without listing messages."),
                AIFunctionFactory.Create(
                    ([Description("New folder name.")] string name,
                        [Description("Parent folder path or alias. Empty creates under the personal namespace root.")] string parentFolder,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        CreateFolderAsync(name, parentFolder, mailboxAlias),
                    name: "create_folder",
                    description:
                        "Creates a new IMAP folder. Confirm the name with the user first."),
                AIFunctionFactory.Create(
                    ([Description(MailboxAliasHint)] string mailboxAlias) => ListMailboxFoldersAsync(mailboxAlias),
                    name: "list_mailbox_folders",
                    description:
                        "Lists IMAP folders (name, path, role) for a connected mailbox account."),
                AIFunctionFactory.Create(
                    (string to, string cc, string bcc, string subject, string body, string htmlBody,
                        [Description("Send mode: new (default), reply, or forward. Reply/forward require reply_uid from a prior get.")] string mode,
                        [Description("Source message Uid for reply/forward. 0 for new mail.")] uint replyUid,
                        [Description("Source folder for reply/forward: empty/inbox (default).")] string replyFolder,
                        [Description("Optional attachments: name|base64;name2|base64 (semicolon between files).")] string attachments,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        SendEmailAsync(to, cc, bcc, subject, body, htmlBody, mode, replyUid, replyFolder, attachments, mailboxAlias),
                    name: "send_email",
                    description:
                        "Sends email via SMTP. Supports to/cc/bcc, plain or HTML body, reply/forward (mode + reply_uid), and attachments (name|base64). Confirm recipients and content with the user first."),
                AIFunctionFactory.Create(
                    (string to, string cc, string bcc, string subject, string body, string htmlBody,
                        [Description("Draft mode: new (default), reply, or forward. Reply/forward require reply_uid from a prior get.")] string mode,
                        [Description("Source message Uid for reply/forward draft. 0 for new draft.")] uint replyUid,
                        [Description("Source folder for reply/forward: empty/inbox (default).")] string replyFolder,
                        [Description("Optional attachments: name|base64;name2|base64 (semicolon between files).")] string attachments,
                        [Description(MailboxAliasHint)] string mailboxAlias) =>
                        SaveDraftAsync(to, cc, bcc, subject, body, htmlBody, mode, replyUid, replyFolder, attachments, mailboxAlias),
                    name: "save_draft",
                    description:
                        "Saves a draft to the Drafts folder. Same shape as send_email but recipients/subject are optional. Reply/forward drafts need reply_uid from a prior get."),
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
            int skip,
            bool countOnly,
            bool unreadOnly,
            string fromSender,
            string subjectContains,
            string bodyContains,
            string toContains,
            string attachmentsFilter,
            string folder,
            string mailboxAlias)
        {
            if (!MailboxListRequestBuilder.TryBuild(
                    since, until, limit, skip, countOnly, unreadOnly, fromSender, subjectContains,
                    bodyContains, toContains, attachmentsFilter, folder,
                    out var listRequest, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.ListMessagesAsync(userId, listRequest!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            if (!listRequest!.CountOnly)
            {
                _lastList = MailboxListSnapshot.From(listRequest, result!, mailboxRef);
            }

            var body = listRequest.CountOnly
                ? EmailMailboxTextHelpers.FormatMailboxCount(result!.TotalMatched, listRequest.QueryLabel)
                : EmailMailboxTextHelpers.FormatMailboxList(result!.Messages, listRequest.QueryLabel, result.TotalMatched);

            return WithAccountHeader(account!, body);
        }

        #endregion

        #region # Queries — Open

        private async Task<string> GetInboxMessageAsync(uint uid, int listIndex, string folder, string mailboxAlias)
        {
            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            if (!MailboxOpenRequestBuilder.TryResolve(uid, listIndex, folder, mailboxRef, _lastList, out var openRequest, out var resolveError))
            {
                return resolveError!;
            }

            var (account, message, error) = await agentService.GetMessageAsync(userId, openRequest!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatMailboxMessage(message!));
        }

        private async Task<string> GetInboxMessagesAsync(string uidsCsv, string folder, string mailboxAlias)
        {
            if (!MessageBatchFiltersBuilder.TryBuild(uidsCsv, folder, out var filters, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, messages, error) = await agentService.GetMessagesAsync(userId, filters!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatMailboxMessages(messages!));
        }

        #endregion

        #region # Queries — Folders & status

        private async Task<string> ListMailboxFoldersAsync(string mailboxAlias)
        {
            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.ListFoldersAsync(userId, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatFolderList(result!.Folders));
        }

        private async Task<string> GetFolderAsync(string folder, string mailboxAlias)
        {
            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var filters = new GetFolderFilters { Folder = NullIfWhiteSpace(folder) };
            var (account, result, error) = await agentService.GetFolderAsync(userId, filters, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatFolderStats(result!));
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

        #region # Queries — Attachments

        private async Task<string> GetAttachmentsAsync(
            uint uid,
            int listIndex,
            string folder,
            int attachmentIndex,
            string attachmentName,
            string mailboxAlias)
        {
            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            if (uid == 0)
            {
                if (!MailboxOpenRequestBuilder.TryResolve(uid, listIndex, folder, mailboxRef, _lastList, out var openRequest, out var resolveError))
                {
                    return resolveError!;
                }

                uid = openRequest!.Message.Uid;
                folder = openRequest.Message.Folder ?? folder;
            }

            if (!GetAttachmentsFiltersBuilder.TryBuild(uid, folder, attachmentIndex, attachmentName, out var filters, out var buildError))
            {
                return buildError!;
            }

            var (account, result, error) = await agentService.GetAttachmentsAsync(userId, filters!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            var folderLabel = string.IsNullOrWhiteSpace(filters!.Message.Folder) ? "inbox" : filters.Message.Folder.Trim();
            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatAttachments(uid, folderLabel, result!.Attachments));
        }

        #endregion

        #region # Commands

        private async Task<string> DeleteMessagesAsync(string uidsCsv, string folder, string mailboxAlias)
        {
            if (!MessageBatchFiltersBuilder.TryBuild(uidsCsv, folder, out var filters, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.DeleteMessagesAsync(userId, filters!, mailboxRef);
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
            if (!MessageTransferFiltersBuilder.TryBuild(uidsCsv, folder, destinationFolder, out var filters, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.MoveMessagesAsync(userId, filters!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatCommandResult(result!));
        }

        private async Task<string> CopyMessagesAsync(
            string uidsCsv,
            string folder,
            string destinationFolder,
            string mailboxAlias)
        {
            if (!MessageTransferFiltersBuilder.TryBuild(uidsCsv, folder, destinationFolder, out var filters, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.CopyMessagesAsync(userId, filters!, mailboxRef);
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
            if (!SetMessageFlagsFiltersBuilder.TryBuild(uidsCsv, folder, flagAction, out var filters, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.SetMessageFlagsAsync(userId, filters!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatCommandResult(result!));
        }

        private async Task<string> SendEmailAsync(
            string to,
            string cc,
            string bcc,
            string subject,
            string body,
            string htmlBody,
            string mode,
            uint replyUid,
            string replyFolder,
            string attachments,
            string mailboxAlias)
        {
            if (!OutboundMailBuilder.TryBuild(
                    to, cc, bcc, subject, body, htmlBody, mode, replyUid, replyFolder, attachments,
                    out var mail, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.SendAsync(userId, mail!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, result!.Message);
        }

        private async Task<string> SaveDraftAsync(
            string to,
            string cc,
            string bcc,
            string subject,
            string body,
            string htmlBody,
            string mode,
            uint replyUid,
            string replyFolder,
            string attachments,
            string mailboxAlias)
        {
            if (!OutboundMailBuilder.TryBuildForDraft(
                    to, cc, bcc, subject, body, htmlBody, mode, replyUid, replyFolder, attachments,
                    out var mail, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.SaveDraftAsync(userId, mail!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatSaveDraftResult(result!));
        }

        private async Task<string> CreateFolderAsync(string name, string parentFolder, string mailboxAlias)
        {
            if (!CreateFolderFiltersBuilder.TryBuild(name, parentFolder, out var filters, out var buildError))
            {
                return buildError!;
            }

            var mailboxRef = EffectiveMailboxAlias(mailboxAlias);
            var (account, result, error) = await agentService.CreateFolderAsync(userId, filters!, mailboxRef);
            if (error is not null)
            {
                return error;
            }

            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatCommandResult(result!));
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
