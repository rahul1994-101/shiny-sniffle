using Application.AI.Memory;
using Application.Features.Shared;
using Application.Features.Workspace.Contacts;
using Application.Features.Workspace.EmailAccounts;
using Infrastructure.Mailbox;
using MediatR.Results;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Net.Mail;

namespace Application.AI.Tools;

public sealed class EmailTriageTools(
    WorkspaceMailboxService mailboxService,
    WorkspaceReferenceService workspaceRefs,
    EmailThreadMemoryService emailMemory,
    EmailAccountRepository emailAccountRepo,
    ContactRepository contactRepo)
{
    #region # Public

    internal EmailTriageToolRun CreateRun(Guid userId, Guid threadId, MailboxAccountContext? defaultMailboxAccount, bool requireMailboxAlias, IReadOnlyList<MailboxListSnapshot> lastLists)
    {
        var session = new Session(mailboxService, workspaceRefs, emailMemory, emailAccountRepo, contactRepo, userId, threadId, defaultMailboxAccount, requireMailboxAlias, lastLists);
        return new EmailTriageToolRun(session.CreateTools(), session.PersistLastListAsync);
    }

    #endregion

    private sealed class Session(
        WorkspaceMailboxService mailboxService,
        WorkspaceReferenceService workspaceRefs,
        EmailThreadMemoryService emailMemory,
        EmailAccountRepository emailAccountRepo,
        ContactRepository contactRepo,
        Guid userId,
        Guid threadId,
        MailboxAccountContext? defaultMailboxAccount,
        bool requireMailboxAlias,
        IReadOnlyList<MailboxListSnapshot> lastLists)
    {
        private const string MailboxAliasHint =
            "Connected mailbox alias, mailbox:alias, or the account email; empty uses @mailbox mention from this turn, else the last inbox listed in this thread, else the default account.";

        private MailboxAccountContext? _defaultAccount = defaultMailboxAccount;
        private readonly string? _stickyAlias = LastUsedAlias(lastLists);
        private Dictionary<string, ContactSummaryDto>? _contactsByEmail;
        private readonly Dictionary<string, MailboxAccountContext> _accountCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MailboxListSnapshot> _lists = ToListMap(lastLists);
        private readonly HashSet<string> _dirtyAliases = new(StringComparer.OrdinalIgnoreCase);
        private int _deepReads;

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
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        CancellationToken cancellationToken) =>
                        ListInboxMessagesAsync(since, until, limit, skip, countOnly, unreadOnly, fromSender, subjectContains, bodyContains, toContains, attachmentsFilter, folder, mailboxAlias, cancellationToken),
                    name: "list_inbox_messages",
                    description:
                        "Lists or counts messages (#N, Uid, from, subject, date, preview) in a mailbox folder. " +
                        $"Since: {sinceHint} " +
                        $"Limit: {limitHint}. skip for pagination (newest-first). " +
                        "Optional filters: unread_only, from_sender, subject_contains, body_contains, to_contains, attachments_filter. " +
                        "Set count_only for how-many questions. Use mailbox_alias when the user names a specific connected account."),
                AIFunctionFactory.Create(
                    ([Description("IMAP UID from a list_inbox_messages row. Use 0 when using list_index.")] uint uid,
                        [Description("1-based list row (#1, #2, …). Use 0 when using uid. Uses the last list for this mailbox in this thread.")] int listIndex,
                        [Description("IMAP folder: empty/inbox (default). Override only when different from the list call.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        CancellationToken cancellationToken) =>
                        GetInboxMessageAsync(uid, listIndex, folder, mailboxAlias, cancellationToken),
                    name: "get_inbox_message",
                    description:
                        "Fetches one message by UID or list row (#N) with full plain-text body and attachment names. " +
                        "After a list in this thread (this turn or saved), open by list_index — do not list again. " +
                        "Or pass uid + folder from a list row."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43). Max 5 per call.")] string uids,
                        [Description("IMAP folder for all Uids: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        CancellationToken cancellationToken) =>
                        GetInboxMessagesAsync(uids, folder, mailboxAlias, cancellationToken),
                    name: "get_inbox_messages",
                    description:
                        $"Fetches up to {MailboxLimits.MaxBatchGetCount} messages by Uid in one folder with full bodies and attachment names. " +
                        "Use the same mailbox_alias as the list call."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43). Max 5 per call.")] string uids,
                        [Description("Source folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        [Description("Must be true after the user explicitly agrees to trash these messages.")] bool confirmed,
                        CancellationToken cancellationToken) =>
                        DeleteMessagesAsync(uids, folder, mailboxAlias, confirmed, cancellationToken),
                    name: "delete_messages",
                    description:
                        $"Moves up to {MailboxLimits.MaxBatchCommandCount} messages to trash (recoverable). " +
                        "Call once to preview, then again with confirmed=true after the user agrees. " +
                        "Use folder + Uids from a recent list with the same mailbox_alias."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43). Max 5 per call.")] string uids,
                        [Description("Source folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description("Destination folder: sent, drafts, trash, junk, archive, or name from list_mailbox_folders.")] string destinationFolder,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        [Description("Must be true after the user agrees to this move.")] bool confirmed,
                        CancellationToken cancellationToken) =>
                        MoveMessagesAsync(uids, folder, destinationFolder, mailboxAlias, confirmed, cancellationToken),
                    name: "move_messages",
                    description:
                        $"Moves up to {MailboxLimits.MaxBatchCommandCount} messages to another folder (archive, junk, custom folder). " +
                        "Call once to preview, then again with confirmed=true after the user agrees."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43). Max 5 per call.")] string uids,
                        [Description("Folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description("Flag action: read, unread, flagged, or unflagged.")] string flagAction,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        CancellationToken cancellationToken) =>
                        SetMessageFlagsAsync(uids, folder, flagAction, mailboxAlias, cancellationToken),
                    name: "set_message_flags",
                    description:
                        $"Updates flags on up to {MailboxLimits.MaxBatchCommandCount} messages: read, unread, flagged, or unflagged."),
                AIFunctionFactory.Create(
                    ([Description("Comma-separated IMAP Uids from list_inbox_messages (e.g. 42,43). Max 5 per call.")] string uids,
                        [Description("Source folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description("Destination folder: sent, drafts, trash, junk, archive, or name from list_mailbox_folders.")] string destinationFolder,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        [Description("Must be true after the user agrees to this copy.")] bool confirmed,
                        CancellationToken cancellationToken) =>
                        CopyMessagesAsync(uids, folder, destinationFolder, mailboxAlias, confirmed, cancellationToken),
                    name: "copy_messages",
                    description:
                        $"Copies up to {MailboxLimits.MaxBatchCommandCount} messages to another folder without removing them from the source. " +
                        "Call once to preview, then again with confirmed=true after the user agrees."),
                AIFunctionFactory.Create(
                    ([Description("IMAP UID from a list/get call. Use 0 when using list_index.")] uint uid,
                        [Description("1-based list row (#1, #2, …). Use 0 when using uid. Uses the last list for this mailbox in this thread.")] int listIndex,
                        [Description("IMAP folder: empty/inbox (default). Override only when different from the list call.")] string folder,
                        [Description("0-based attachment index from get_inbox_message output. Use -1 when using attachment_name or to fetch all.")] int attachmentIndex,
                        [Description("Attachment file name (case-insensitive). Empty when using attachment_index or fetching all.")] string attachmentName,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        CancellationToken cancellationToken) =>
                        GetAttachmentsAsync(uid, listIndex, folder, attachmentIndex, attachmentName, mailboxAlias, cancellationToken),
                    name: "get_attachments",
                    description:
                        "Downloads attachment content for one message. Returns metadata and text preview for small text files; binary/large files show size only. " +
                        "Use uid+folder from a list row, or list_index from the last list in this thread. Optionally filter by attachment_index or attachment_name."),
                AIFunctionFactory.Create(
                    ([Description("IMAP folder: empty/inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders.")] string folder,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        CancellationToken cancellationToken) =>
                        GetFolderAsync(folder, mailboxAlias, cancellationToken),
                    name: "get_folder",
                    description:
                        "Returns folder stats: total message count, unread count, and Uid validity. Use for how-many-unread without listing messages."),
                AIFunctionFactory.Create(
                    ([Description("New folder name.")] string name,
                        [Description("Parent folder path or alias. Empty creates under the personal namespace root.")] string parentFolder,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        [Description("Must be true after the user agrees to create this folder.")] bool confirmed,
                        CancellationToken cancellationToken) =>
                        CreateFolderAsync(name, parentFolder, mailboxAlias, confirmed, cancellationToken),
                    name: "create_folder",
                    description:
                        "Creates a new IMAP folder. Call once to preview, then again with confirmed=true after the user agrees."),
                AIFunctionFactory.Create(
                    ([Description(MailboxAliasHint)] string mailboxAlias, CancellationToken cancellationToken) =>
                        ListMailboxFoldersAsync(mailboxAlias, cancellationToken),
                    name: "list_mailbox_folders",
                    description:
                        "Lists IMAP folders (name, path, role) for a connected mailbox account."),
                AIFunctionFactory.Create(
                    ([Description("Primary recipients: emails and/or contact:alias (comma-separated).")] string to,
                        [Description("CC recipients: emails and/or contact:alias. Empty means none.")] string cc,
                        [Description("BCC recipients: emails and/or contact:alias. Empty means none.")] string bcc,
                        [Description("Subject line. Required for new mail.")] string subject,
                        [Description("Plain-text body.")] string body,
                        [Description("Optional HTML body. Empty uses plain text only.")] string htmlBody,
                        [Description("Send mode: new (default), reply, or forward. Reply/forward require reply_uid or list_index from a prior list/get.")] string mode,
                        [Description("Source message Uid for reply/forward. 0 when using list_index or for new mail.")] uint replyUid,
                        [Description("1-based list row for reply/forward when reply_uid is 0. Uses the last list for this mailbox in this thread.")] int listIndex,
                        [Description("Source folder for reply/forward: empty/inbox (default).")] string replyFolder,
                        [Description("Optional attachments: name|base64;name2|base64 (semicolon between files).")] string attachments,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        [Description("Must be true after the user explicitly agrees to send this message.")] bool confirmed,
                        CancellationToken cancellationToken) =>
                        SendEmailAsync(to, cc, bcc, subject, body, htmlBody, mode, replyUid, listIndex, replyFolder, attachments, mailboxAlias, confirmed, cancellationToken),
                    name: "send_email",
                    description:
                        "Sends email via SMTP. Recipients may be emails or contact:alias (search_contacts first if needed). " +
                        "Call once to preview, then again with confirmed=true after the user agrees. " +
                        "Supports reply/forward (mode + reply_uid or list_index) and attachments (name|base64)."),
                AIFunctionFactory.Create(
                    ([Description("Primary recipients, comma-separated emails. Optional for drafts.")] string to,
                        [Description("CC recipients, comma-separated. Empty means none.")] string cc,
                        [Description("BCC recipients, comma-separated. Empty means none.")] string bcc,
                        [Description("Subject line. Optional for drafts.")] string subject,
                        [Description("Plain-text body.")] string body,
                        [Description("Optional HTML body. Empty uses plain text only.")] string htmlBody,
                        [Description("Draft mode: new (default), reply, or forward. Reply/forward require reply_uid or list_index.")] string mode,
                        [Description("Source message Uid for reply/forward draft. 0 when using list_index or for new draft.")] uint replyUid,
                        [Description("1-based list row for reply/forward when reply_uid is 0. Uses the last list for this mailbox in this thread.")] int listIndex,
                        [Description("Source folder for reply/forward: empty/inbox (default).")] string replyFolder,
                        [Description("Optional attachments: name|base64;name2|base64 (semicolon between files).")] string attachments,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        CancellationToken cancellationToken) =>
                        SaveDraftAsync(to, cc, bcc, subject, body, htmlBody, mode, replyUid, listIndex, replyFolder, attachments, mailboxAlias, cancellationToken),
                    name: "save_draft",
                    description:
                        "Saves a draft to the Drafts folder. Same shape as send_email but recipients/subject are optional. Reply/forward drafts need reply_uid or list_index from a prior list/get."),
                AIFunctionFactory.Create(
                    (CancellationToken cancellationToken) =>
                        ListEmailAccountsAsync(cancellationToken),
                    name: "list_email_accounts",
                    description:
                        "Lists workspace-connected email accounts (alias, address, provider, default, notes). " +
                        "Use for how-many / which accounts. Does not test IMAP — use get_mailbox_status for reachability, " +
                        "or summarize_all_inboxes for unread counts on every account."),
                AIFunctionFactory.Create(
                    ([Description("IMAP folder: empty/inbox (default), sent, drafts, trash, junk, or a folder name.")] string folder,
                        CancellationToken cancellationToken) =>
                        SummarizeAllInboxesAsync(folder, cancellationToken),
                    name: "summarize_all_inboxes",
                    description:
                        "Inbox (or folder) totals and unread counts for every connected account. " +
                        "Use when the user asks about all inboxes / both accounts / every mailbox. Does not list message rows."),
                AIFunctionFactory.Create(
                    ([Description(MailboxAliasHint)] string mailboxAlias, CancellationToken cancellationToken) =>
                        GetMailboxStatusAsync(mailboxAlias, cancellationToken),
                    name: "get_mailbox_status",
                    description:
                        "Checks whether one mailbox account is configured and IMAP/SMTP are reachable. " +
                        "Not for counting connected accounts — use list_email_accounts."),
                AIFunctionFactory.Create(
                    ([Description("First period since keyword or range (e.g. today, this_week).")] string firstSince,
                        [Description("Second period since keyword or range (e.g. yesterday, last_week).")] string secondSince,
                        [Description("IMAP folder: empty/inbox (default). Same folder for both periods.")] string folder,
                        [Description("When true, only unread. Same filter on both periods.")] bool unreadOnly,
                        [Description("Sender filter applied to both periods. Empty means none.")] string fromSender,
                        [Description(MailboxAliasHint)] string mailboxAlias,
                        CancellationToken cancellationToken) =>
                        CompareMailPeriodsAsync(firstSince, secondSince, folder, unreadOnly, fromSender, mailboxAlias, cancellationToken),
                    name: "compare_mail_periods",
                    description:
                        "Compares message counts for two time ranges (same folder and filters). Use for more/less-than questions (today vs yesterday, this_week vs last_week)."),
                AIFunctionFactory.Create(
                    ([Description("Name, alias, or email to search. Empty lists recent contacts.")] string query,
                        CancellationToken cancellationToken) =>
                        SearchContactsAsync(query, cancellationToken),
                    name: "search_contacts",
                    description:
                        "Looks up workspace contacts by name, alias, or email. Returns notes (context) when set — use them for tone and facts. Use before send_email when the user names a person instead of an address."),
                AIFunctionFactory.Create(
                    ([Description("First name.")] string firstName,
                        [Description("Last name. Empty is allowed.")] string lastName,
                        [Description("Email address.")] string email,
                        [Description("Optional notes the agent should use when dealing with this person.")] string context,
                        [Description("Must be true after the user agrees to save this person.")] bool confirmed,
                        CancellationToken cancellationToken) =>
                        SaveContactAsync(firstName, lastName, email, context, confirmed, cancellationToken),
                    name: "save_contact",
                    description:
                        "Saves a workspace contact from a sender or name the user wants to remember. Confirm with the user first, then call with confirmed=true.")
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
            string mailboxAlias,
            CancellationToken cancellationToken)
        {
            var (resolvedFrom, fromError) = await ResolveFromFilterAsync(fromSender, cancellationToken);
            if (fromError is not null)
            {
                return fromError;
            }

            if (!ListMessagesQueryBuilder.TryBuild(
                    since, until, limit, skip, countOnly, unreadOnly, resolvedFrom, subjectContains,
                    bodyContains, toContains, attachmentsFilter, folder,
                    out var query, out var buildError))
            {
                return buildError!;
            }

            var (account, accountError) = await GetAccountAsync(mailboxAlias, cancellationToken);
            if (accountError is not null)
            {
                return accountError;
            }

            var outcome = await mailboxService.ListMessagesAsync(account!, query!.Filters, cancellationToken);
            if (outcome.HasError)
            {
                return outcome.FirstErrorMessage!;
            }

            if (!query.Filters.CountOnly)
            {
                RememberList(MailboxListSnapshot.From(query.Filters, outcome.Payload!, account!.Alias, query.QueryLabel));
            }

            var body = query.Filters.CountOnly
                ? EmailMailboxTextHelpers.FormatMailboxCount(outcome.Payload!.TotalMatched, query.QueryLabel)
                : EmailMailboxTextHelpers.FormatMailboxList(outcome.Payload!.Messages, query.QueryLabel, outcome.Payload.TotalMatched);

            var headers = query.Filters.CountOnly
                ? (IEnumerable<string>)[resolvedFrom]
                : outcome.Payload!.Messages.Select(m => m.From);
            body = await AppendKnownContactsAsync(body, headers, cancellationToken);

            return WithAccountHeader(account!, body);
        }

        #endregion

        #region # Queries — Open

        private async Task<string> GetInboxMessageAsync(uint uid, int listIndex, string folder, string mailboxAlias, CancellationToken cancellationToken)
        {
            var (account, accountError) = await GetAccountAsync(mailboxAlias, cancellationToken);
            if (accountError is not null)
            {
                return accountError;
            }

            if (!MailboxOpenRequestBuilder.TryResolve(uid, listIndex, folder, account!.Alias, ListFor(account.Alias), out var openRequest, out var resolveError))
            {
                return resolveError!;
            }

            var deepReadError = TryConsumeDeepReads(1);
            if (deepReadError is not null)
            {
                return deepReadError;
            }

            var outcome = await mailboxService.GetMessageAsync(account, openRequest!.Message, cancellationToken);
            if (outcome.HasError)
            {
                return outcome.FirstErrorMessage!;
            }

            var message = outcome.Payload!;
            var body = await AppendKnownContactsAsync(
                EmailMailboxTextHelpers.FormatMailboxMessage(message),
                ContactHeaders(message),
                cancellationToken);
            return WithAccountHeader(account, body);
        }

        private async Task<string> GetInboxMessagesAsync(string uidsCsv, string folder, string mailboxAlias, CancellationToken cancellationToken)
        {
            if (!MessageBatchFiltersBuilder.TryBuild(uidsCsv, folder, MailboxLimits.MaxBatchGetCount, out var filters, out var buildError))
            {
                return buildError!;
            }

            var (account, accountError) = await GetAccountAsync(mailboxAlias, cancellationToken);
            if (accountError is not null)
            {
                return accountError;
            }

            var deepReadError = TryConsumeDeepReads(filters!.Messages.Count);
            if (deepReadError is not null)
            {
                return deepReadError;
            }

            var outcome = await mailboxService.GetMessagesAsync(account!, filters, cancellationToken);
            if (outcome.HasError)
            {
                return outcome.FirstErrorMessage!;
            }

            var messages = outcome.Payload!.Messages;
            var body = await AppendKnownContactsAsync(
                EmailMailboxTextHelpers.FormatMailboxMessages(messages),
                messages.SelectMany(ContactHeaders),
                cancellationToken);
            return WithAccountHeader(account!, body);
        }

        #endregion

        #region # Queries — Folders & status

        private Task<string> ListMailboxFoldersAsync(string mailboxAlias, CancellationToken cancellationToken)
        {
            return RunMailboxAsync(
                mailboxAlias,
                account => mailboxService.ListFoldersAsync(account, cancellationToken),
                result => EmailMailboxTextHelpers.FormatFolderList(result.Folders),
                cancellationToken);
        }

        private Task<string> GetFolderAsync(string folder, string mailboxAlias, CancellationToken cancellationToken)
        {
            var filters = new GetFolderFilters { Folder = NullIfWhiteSpace(folder) };
            return RunMailboxAsync(
                mailboxAlias,
                account => mailboxService.GetFolderAsync(account, filters, cancellationToken),
                EmailMailboxTextHelpers.FormatFolderStats,
                cancellationToken);
        }

        private async Task<string> ListEmailAccountsAsync(CancellationToken cancellationToken)
        {
            var accounts = await emailAccountRepo.GetAllEmailAccountsByUserIdAsync(userId, cancellationToken);
            if (accounts.Count == 0)
            {
                return "No email accounts connected. Add one in Workspace → Email accounts.";
            }

            return $"Connected email accounts: {accounts.Count}\n" + string.Join('\n', accounts.Select(EmailAccountMapping.FormatAgentLine));
        }

        private async Task<string> SummarizeAllInboxesAsync(string folder, CancellationToken cancellationToken)
        {
            var accounts = await emailAccountRepo.GetAllEmailAccountsByUserIdAsync(userId, cancellationToken);
            if (accounts.Count == 0)
            {
                return "No email accounts connected. Add one in Workspace → Email accounts.";
            }

            var folderLabel = string.IsNullOrWhiteSpace(folder) ? "inbox" : folder.Trim();
            var resolved = new List<(EmailAccountSummaryDto Summary, MailboxAccountContext? Account, string? Error)>(accounts.Count);
            foreach (var summary in accounts)
            {
                var (account, accountError) = await GetAccountAsync(summary.Alias, cancellationToken);
                resolved.Add((summary, account, accountError));
            }

            var filters = new GetFolderFilters { Folder = NullIfWhiteSpace(folder) };
            var lines = await Task.WhenAll(resolved.Select(async item =>
            {
                var summary = item.Summary;
                if (item.Error is not null)
                {
                    return $"- {summary.Alias} ({summary.EmailAddress}): {item.Error}";
                }

                var outcome = await mailboxService.GetFolderAsync(item.Account!, filters, cancellationToken);
                if (outcome.HasError)
                {
                    return $"- {summary.Alias} ({summary.EmailAddress}): {outcome.FirstErrorMessage}";
                }

                var stats = outcome.Payload!;
                var mark = summary.IsDefault ? " · default" : string.Empty;
                return
                    $"- {summary.Alias} ({summary.EmailAddress}){mark} — {stats.UnreadCount} unread / {stats.TotalCount} total{CatalogFieldRules.FormatNotesSuffix(summary.Context)}";
            }));

            return $"Folder stats for {accounts.Count} account(s) ({folderLabel}):\n" + string.Join('\n', lines);
        }

        private Task<string> GetMailboxStatusAsync(string mailboxAlias, CancellationToken cancellationToken)
        {
            return RunMailboxAsync(
                mailboxAlias,
                account => mailboxService.TestConnectionAsync(account, cancellationToken),
                status => status!.Message,
                cancellationToken);
        }

        #endregion

        #region # Queries — Attachments

        private async Task<string> GetAttachmentsAsync(
            uint uid,
            int listIndex,
            string folder,
            int attachmentIndex,
            string attachmentName,
            string mailboxAlias,
            CancellationToken cancellationToken)
        {
            var (account, accountError) = await GetAccountAsync(mailboxAlias, cancellationToken);
            if (accountError is not null)
            {
                return accountError;
            }

            if (uid == 0)
            {
                if (!MailboxOpenRequestBuilder.TryResolve(uid, listIndex, folder, account!.Alias, ListFor(account.Alias), out var openRequest, out var resolveError))
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

            var outcome = await mailboxService.GetAttachmentsAsync(account!, filters!, cancellationToken);
            if (outcome.HasError)
            {
                return outcome.FirstErrorMessage!;
            }

            var folderLabel = string.IsNullOrWhiteSpace(filters!.Message.Folder) ? "inbox" : filters.Message.Folder.Trim();
            return WithAccountHeader(account!, EmailMailboxTextHelpers.FormatAttachments(uid, folderLabel, outcome.Payload!.Attachments));
        }

        #endregion

        #region # Commands

        private Task<string> DeleteMessagesAsync(string uidsCsv, string folder, string mailboxAlias, bool confirmed, CancellationToken cancellationToken)
        {
            if (!MessageBatchFiltersBuilder.TryBuild(uidsCsv, folder, out var filters, out var buildError))
            {
                return Task.FromResult(buildError!);
            }

            var gate = RequireConfirmed(confirmed, $"Trash Uid(s) {uidsCsv} from '{NullIfWhiteSpace(folder) ?? "inbox"}' (recoverable).");
            if (gate is not null)
            {
                return Task.FromResult(gate);
            }

            return RunMailboxAsync(
                mailboxAlias,
                account => mailboxService.DeleteMessagesAsync(account, filters!, cancellationToken),
                EmailMailboxTextHelpers.FormatCommandResult,
                cancellationToken);
        }

        private Task<string> MoveMessagesAsync(string uidsCsv, string folder, string destinationFolder, string mailboxAlias, bool confirmed, CancellationToken cancellationToken)
        {
            if (!MessageTransferFiltersBuilder.TryBuild(uidsCsv, folder, destinationFolder, out var filters, out var buildError))
            {
                return Task.FromResult(buildError!);
            }

            var gate = RequireConfirmed(confirmed, $"Move Uid(s) {uidsCsv} from '{NullIfWhiteSpace(folder) ?? "inbox"}' to '{destinationFolder}'.");
            if (gate is not null)
            {
                return Task.FromResult(gate);
            }

            return RunMailboxAsync(
                mailboxAlias,
                account => mailboxService.MoveMessagesAsync(account, filters!, cancellationToken),
                EmailMailboxTextHelpers.FormatCommandResult,
                cancellationToken);
        }

        private Task<string> CopyMessagesAsync(string uidsCsv, string folder, string destinationFolder, string mailboxAlias, bool confirmed, CancellationToken cancellationToken)
        {
            if (!MessageTransferFiltersBuilder.TryBuild(uidsCsv, folder, destinationFolder, out var filters, out var buildError))
            {
                return Task.FromResult(buildError!);
            }

            var gate = RequireConfirmed(confirmed, $"Copy Uid(s) {uidsCsv} from '{NullIfWhiteSpace(folder) ?? "inbox"}' to '{destinationFolder}'.");
            if (gate is not null)
            {
                return Task.FromResult(gate);
            }

            return RunMailboxAsync(
                mailboxAlias,
                account => mailboxService.CopyMessagesAsync(account, filters!, cancellationToken),
                EmailMailboxTextHelpers.FormatCommandResult,
                cancellationToken);
        }

        private Task<string> SetMessageFlagsAsync(string uidsCsv, string folder, string flagAction, string mailboxAlias, CancellationToken cancellationToken)
        {
            if (!SetMessageFlagsFiltersBuilder.TryBuild(uidsCsv, folder, flagAction, out var filters, out var buildError))
            {
                return Task.FromResult(buildError!);
            }

            return RunMailboxAsync(
                mailboxAlias,
                account => mailboxService.SetMessageFlagsAsync(account, filters!, cancellationToken),
                EmailMailboxTextHelpers.FormatCommandResult,
                cancellationToken);
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
            int listIndex,
            string replyFolder,
            string attachments,
            string mailboxAlias,
            bool confirmed,
            CancellationToken cancellationToken)
        {
            var (account, accountError) = await GetAccountAsync(mailboxAlias, cancellationToken);
            if (accountError is not null)
            {
                return accountError;
            }

            if (!TryResolveReplyTarget(mode, replyUid, listIndex, replyFolder, account!.Alias, out replyUid, out replyFolder, out var resolveError))
            {
                return resolveError!;
            }

            var (resolvedTo, toError) = await ResolveAddressListAsync(to, "to", cancellationToken);
            if (toError is not null)
            {
                return toError;
            }

            var (resolvedCc, ccError) = await ResolveAddressListAsync(cc, "cc", cancellationToken);
            if (ccError is not null)
            {
                return ccError;
            }

            var (resolvedBcc, bccError) = await ResolveAddressListAsync(bcc, "bcc", cancellationToken);
            if (bccError is not null)
            {
                return bccError;
            }

            if (!OutboundMailBuilder.TryBuild(
                    resolvedTo!, resolvedCc!, resolvedBcc!, subject, body, htmlBody, mode, replyUid, replyFolder, attachments,
                    out var mail, out var buildError))
            {
                return buildError!;
            }

            var gate = RequireConfirmed(
                confirmed,
                $"Send {mail!.Mode} mail to '{mail.To}'" +
                (mail.Cc.Count > 0 ? $", cc '{string.Join(", ", mail.Cc)}'" : string.Empty) +
                $", subject '{mail.Subject}'.");
            if (gate is not null)
            {
                return gate;
            }

            return await RunMailboxAsync(
                account,
                resolvedAccount => mailboxService.SendAsync(resolvedAccount, mail!, cancellationToken),
                result => result!.Message);
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
            int listIndex,
            string replyFolder,
            string attachments,
            string mailboxAlias,
            CancellationToken cancellationToken)
        {
            var (account, accountError) = await GetAccountAsync(mailboxAlias, cancellationToken);
            if (accountError is not null)
            {
                return accountError;
            }

            if (!TryResolveReplyTarget(mode, replyUid, listIndex, replyFolder, account!.Alias, out replyUid, out replyFolder, out var resolveError))
            {
                return resolveError!;
            }

            var (resolvedTo, toError) = await ResolveAddressListAsync(to, "to", cancellationToken);
            if (toError is not null)
            {
                return toError;
            }

            var (resolvedCc, ccError) = await ResolveAddressListAsync(cc, "cc", cancellationToken);
            if (ccError is not null)
            {
                return ccError;
            }

            var (resolvedBcc, bccError) = await ResolveAddressListAsync(bcc, "bcc", cancellationToken);
            if (bccError is not null)
            {
                return bccError;
            }

            if (!OutboundMailBuilder.TryBuildForDraft(
                    resolvedTo!, resolvedCc!, resolvedBcc!, subject, body, htmlBody, mode, replyUid, replyFolder, attachments,
                    out var mail, out var buildError))
            {
                return buildError!;
            }

            return await RunMailboxAsync(
                account,
                resolvedAccount => mailboxService.SaveDraftAsync(resolvedAccount, mail!, cancellationToken),
                EmailMailboxTextHelpers.FormatSaveDraftResult);
        }

        private Task<string> CreateFolderAsync(string name, string parentFolder, string mailboxAlias, bool confirmed, CancellationToken cancellationToken)
        {
            if (!CreateFolderFiltersBuilder.TryBuild(name, parentFolder, out var filters, out var buildError))
            {
                return Task.FromResult(buildError!);
            }

            var parent = string.IsNullOrWhiteSpace(parentFolder) ? "root" : parentFolder.Trim();
            var gate = RequireConfirmed(confirmed, $"Create folder '{name}' under '{parent}'.");
            if (gate is not null)
            {
                return Task.FromResult(gate);
            }

            return RunMailboxAsync(
                mailboxAlias,
                account => mailboxService.CreateFolderAsync(account, filters!, cancellationToken),
                EmailMailboxTextHelpers.FormatCommandResult,
                cancellationToken);
        }

        #endregion

        #region # Workspace — contacts & compare

        private async Task<string> CompareMailPeriodsAsync(
            string firstSince,
            string secondSince,
            string folder,
            bool unreadOnly,
            string fromSender,
            string mailboxAlias,
            CancellationToken cancellationToken)
        {
            var (resolvedFrom, fromError) = await ResolveFromFilterAsync(fromSender, cancellationToken);
            if (fromError is not null)
            {
                return fromError;
            }

            if (!ListMessagesQueryBuilder.TryBuild(
                    firstSince, string.Empty, MailboxLimits.DefaultListLimit, 0, countOnly: true, unreadOnly, resolvedFrom,
                    string.Empty, string.Empty, string.Empty, string.Empty, folder,
                    out var firstQuery, out var firstError))
            {
                return firstError!;
            }

            if (!ListMessagesQueryBuilder.TryBuild(
                    secondSince, string.Empty, MailboxLimits.DefaultListLimit, 0, countOnly: true, unreadOnly, resolvedFrom,
                    string.Empty, string.Empty, string.Empty, string.Empty, folder,
                    out var secondQuery, out var secondError))
            {
                return secondError!;
            }

            var (account, accountError) = await GetAccountAsync(mailboxAlias, cancellationToken);
            if (accountError is not null)
            {
                return accountError;
            }

            var firstOutcome = await mailboxService.ListMessagesAsync(account!, firstQuery!.Filters, cancellationToken);
            if (firstOutcome.HasError)
            {
                return firstOutcome.FirstErrorMessage!;
            }

            var secondOutcome = await mailboxService.ListMessagesAsync(account!, secondQuery!.Filters, cancellationToken);
            if (secondOutcome.HasError)
            {
                return secondOutcome.FirstErrorMessage!;
            }

            var compare = EmailMailboxTextHelpers.FormatMailboxCompare(
                firstQuery.QueryLabel,
                firstOutcome.Payload!.TotalMatched,
                secondQuery.QueryLabel,
                secondOutcome.Payload!.TotalMatched);
            compare = await AppendKnownContactsAsync(compare, [resolvedFrom], cancellationToken);
            return WithAccountHeader(account!, compare);
        }

        private async Task<string> SearchContactsAsync(string query, CancellationToken cancellationToken)
        {
            var matches = await contactRepo.SearchContactsForAIAsync(userId, query ?? string.Empty, 8, cancellationToken);
            if (matches.Count == 0)
            {
                return string.IsNullOrWhiteSpace(query)
                    ? "No workspace contacts yet. Add one in Workspace → Contacts or use save_contact."
                    : $"No contacts match '{query.Trim()}'.";
            }

            return "Contacts:\n" + string.Join('\n', matches.Select(ContactMapping.FormatAgentLine));
        }

        private async Task<string> SaveContactAsync(string firstName, string lastName, string email, string context, bool confirmed, CancellationToken cancellationToken)
        {
            var dto = new SaveContactDto
            {
                FirstName = firstName ?? string.Empty,
                LastName = lastName ?? string.Empty,
                Email = email,
                Context = context
            };

            var validation = ContactMapping.ValidateSave(dto);
            if (validation is not null)
            {
                return validation;
            }

            var gate = RequireConfirmed(confirmed, $"Save contact {firstName} {lastName} <{email}>.");
            if (gate is not null)
            {
                return gate;
            }

            var (saved, error, _) = await contactRepo.SaveAsync(userId, dto, userId, cancellationToken, ContactSource.FromEmail);
            if (error is not null)
            {
                return error;
            }

            if (saved is null)
            {
                return "Could not save the contact.";
            }

            return $"Saved contact {ContactMapping.FormatAgentPhrase(new ContactSummaryDto
            {
                Id = saved.Id,
                ListLabel = saved.ListLabel,
                Alias = saved.Alias,
                Email = saved.Email,
                Context = saved.Context
            })}.";
        }

        #endregion

        #region # Session helpers

        internal async Task PersistLastListAsync(CancellationToken cancellationToken)
        {
            foreach (var alias in _dirtyAliases)
            {
                if (_lists.TryGetValue(alias, out var snapshot))
                {
                    await emailMemory.SaveLastListAsync(userId, threadId, snapshot, cancellationToken);
                }
            }
        }

        private static string? LastUsedAlias(IReadOnlyList<MailboxListSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                var alias = snapshot.MailboxAlias?.Trim();
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    return alias;
                }
            }

            return null;
        }

        private static Dictionary<string, MailboxListSnapshot> ToListMap(IReadOnlyList<MailboxListSnapshot> snapshots)
        {
            var map = new Dictionary<string, MailboxListSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (var snapshot in snapshots)
            {
                var alias = snapshot.MailboxAlias?.Trim();
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    map[alias] = snapshot;
                }
            }

            return map;
        }

        private void RememberList(MailboxListSnapshot snapshot)
        {
            var alias = snapshot.MailboxAlias?.Trim();
            if (string.IsNullOrWhiteSpace(alias))
            {
                return;
            }

            _lists[alias] = snapshot;
            _dirtyAliases.Add(alias);
        }

        private MailboxListSnapshot? ListFor(string? mailboxAlias)
        {
            var alias = mailboxAlias?.Trim();
            return !string.IsNullOrWhiteSpace(alias) && _lists.TryGetValue(alias, out var snapshot)
                ? snapshot
                : null;
        }

        private static string? RequireConfirmed(bool confirmed, string actionSummary)
        {
            return confirmed
                ? null
                : "Confirmation required. Tell the user this plan, then call again with confirmed=true only after they agree.\n" + actionSummary;
        }

        private async Task<(string Filter, string? Error)> ResolveFromFilterAsync(string fromSender, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fromSender))
            {
                return (string.Empty, null);
            }

            var trimmed = fromSender.Trim();
            if (MailAddress.TryCreate(trimmed, out var parsed))
            {
                return (parsed.Address, null);
            }

            var handle = trimmed.TrimStart('@');
            if (handle.StartsWith("contact:", StringComparison.OrdinalIgnoreCase))
            {
                var alias = handle["contact:".Length..];
                var byAlias = await contactRepo.GetContactByAliasAsync(userId, alias, cancellationToken);
                if (byAlias is null)
                {
                    return (string.Empty, $"from_sender: no contact found for {EntityRefs.Format(EntityRefs.Kind.Contact, alias)}.");
                }

                if (string.IsNullOrWhiteSpace(byAlias.Email))
                {
                    return (string.Empty, $"from_sender: {byAlias.EntityRef} has no email.");
                }

                return (byAlias.Email, null);
            }

            var matches = await contactRepo.SearchContactsForAIAsync(userId, trimmed, 5, cancellationToken);
            var withEmail = matches.Where(c => !string.IsNullOrWhiteSpace(c.Email)).ToList();
            return withEmail.Count == 1
                ? (withEmail[0].Email!, null)
                : (trimmed, null);
        }

        private async Task<string> AppendKnownContactsAsync(string body, IEnumerable<string> headers, CancellationToken cancellationToken)
        {
            var map = await EnsureContactsByEmailAsync(cancellationToken);
            if (map.Count == 0)
            {
                return body;
            }

            var matched = new List<ContactSummaryDto>();
            var seen = new HashSet<Guid>();
            foreach (var header in headers)
            {
                var email = TryExtractEmail(header);
                if (email is null || !map.TryGetValue(email, out var contact) || !seen.Add(contact.Id))
                {
                    continue;
                }

                matched.Add(contact);
            }

            if (matched.Count == 0)
            {
                return body;
            }

            return body + "\n\nKnown contacts in this result:\n" + string.Join('\n', matched.Select(ContactMapping.FormatAgentLine));
        }

        private async Task<Dictionary<string, ContactSummaryDto>> EnsureContactsByEmailAsync(CancellationToken cancellationToken)
        {
            if (_contactsByEmail is not null)
            {
                return _contactsByEmail;
            }

            var contacts = await contactRepo.ListForAgentAsync(userId, cancellationToken);
            _contactsByEmail = new Dictionary<string, ContactSummaryDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var contact in contacts)
            {
                var email = ContactMapping.NormalizeEmail(contact.Email);
                if (email is not null)
                {
                    _contactsByEmail[email] = contact;
                }
            }

            return _contactsByEmail;
        }

        private static IEnumerable<string> ContactHeaders(MessageDetail message)
        {
            yield return message.From;
            foreach (var to in message.To)
            {
                yield return to;
            }

            foreach (var cc in message.Cc)
            {
                yield return cc;
            }
        }

        private static string? TryExtractEmail(string? header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return null;
            }

            return MailAddress.TryCreate(header.Trim(), out var parsed)
                ? ContactMapping.NormalizeEmail(parsed.Address)
                : null;
        }

        private async Task<(string Resolved, string? Error)> ResolveAddressListAsync(string? raw, string field, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return (string.Empty, null);
            }

            var resolved = new List<string>();
            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var (address, error) = await ResolveOneAddressAsync(part, field, cancellationToken);
                if (error is not null)
                {
                    return (string.Empty, error);
                }

                resolved.Add(address!);
            }

            return (string.Join(", ", resolved), null);
        }

        private async Task<(string? Address, string? Error)> ResolveOneAddressAsync(string part, string field, CancellationToken cancellationToken)
        {
            if (MailAddress.TryCreate(part, out var parsed))
            {
                return (parsed.Address, null);
            }

            var handle = part.Trim().TrimStart('@');
            if (handle.StartsWith("contact:", StringComparison.OrdinalIgnoreCase))
            {
                var alias = handle["contact:".Length..];
                var byAlias = await contactRepo.GetContactByAliasAsync(userId, alias, cancellationToken);
                if (byAlias is null)
                {
                    return (null, $"{field}: no contact found for {EntityRefs.Format(EntityRefs.Kind.Contact, alias)}.");
                }

                if (string.IsNullOrWhiteSpace(byAlias.Email))
                {
                    return (null, $"{field}: {byAlias.EntityRef} has no email. Ask the user for an address or update the contact.");
                }

                return (byAlias.Email, null);
            }

            var matches = await contactRepo.SearchContactsForAIAsync(userId, part, 5, cancellationToken);
            var withEmail = matches.Where(c => !string.IsNullOrWhiteSpace(c.Email)).ToList();
            if (withEmail.Count == 1)
            {
                return (withEmail[0].Email, null);
            }

            if (withEmail.Count > 1)
            {
                var options = string.Join(", ", withEmail.Select(c => $"{c.EntityRef} <{c.Email}>"));
                return (null, $"{field}: several contacts match '{part}' ({options}). Use contact:alias or the exact email.");
            }

            return (null, $"{field} contains an invalid email or unknown contact: '{part}'. Use search_contacts or an email address.");
        }

        private async Task<(MailboxAccountContext? Account, string? Error)> GetAccountAsync(string? mailboxAlias, CancellationToken cancellationToken)
        {
            if (_defaultAccount is not null)
            {
                CacheAccount(_defaultAccount);
            }

            var alias = NullIfWhiteSpace(mailboxAlias) ?? _defaultAccount?.Alias ?? _stickyAlias;

            if (alias is null)
            {
                if (requireMailboxAlias)
                {
                    return (null, "Pass mailbox_alias for this call. A mailbox mention was ambiguous or did not resolve.");
                }

                if (_defaultAccount is not null)
                {
                    return (_defaultAccount, null);
                }

                var defaultOutcome = await workspaceRefs.TryResolveMailboxAsync(userId, null, cancellationToken);
                if (defaultOutcome.HasError)
                {
                    return (null, defaultOutcome.FirstErrorMessage);
                }

                _defaultAccount = defaultOutcome.Payload;
                CacheAccount(_defaultAccount!);
                return (_defaultAccount, null);
            }

            if (_accountCache.TryGetValue(alias, out var cached))
            {
                return (cached, null);
            }

            if (_defaultAccount is not null && AccountMatches(_defaultAccount, alias))
            {
                CacheAccount(_defaultAccount, alias);
                return (_defaultAccount, null);
            }

            var resolved = await workspaceRefs.TryResolveMailboxAsync(userId, alias, cancellationToken);
            if (resolved.HasError)
            {
                return (null, resolved.FirstErrorMessage);
            }

            CacheAccount(resolved.Payload!, alias);
            return (resolved.Payload, null);
        }

        private void CacheAccount(MailboxAccountContext account, string? lookupKey = null)
        {
            _accountCache[account.Alias] = account;
            _accountCache[EntityRefs.Format(EntityRefs.Kind.Mailbox, account.Alias)] = account;
            if (!string.IsNullOrWhiteSpace(account.EmailAddress))
            {
                _accountCache[account.EmailAddress] = account;
            }

            var key = NullIfWhiteSpace(lookupKey);
            if (key is not null)
            {
                _accountCache[key] = account;
            }
        }

        private static bool AccountMatches(MailboxAccountContext account, string alias) =>
            string.Equals(account.Alias, alias, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(account.EmailAddress, alias, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(EntityRefs.Format(EntityRefs.Kind.Mailbox, account.Alias), alias, StringComparison.OrdinalIgnoreCase);

        private static string WithAccountHeader(MailboxAccountContext account, string body)
        {
            return $"{EmailReadConstants.FormatMailboxHeader(account)}\n{body}";
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private string? TryConsumeDeepReads(int count)
        {
            if (_deepReads + count > EmailReadConstants.MaxDeepReadsPerTurn)
            {
                return
                    $"This turn already used {_deepReads} full-read(s); max is {EmailReadConstants.MaxDeepReadsPerTurn}. " +
                    "Summarize from the list or ask the user to continue in a new message.";
            }

            _deepReads += count;
            return null;
        }

        private bool TryResolveReplyTarget(
            string mode,
            uint replyUid,
            int listIndex,
            string replyFolder,
            string accountAlias,
            out uint uid,
            out string folder,
            out string? error)
        {
            uid = replyUid;
            folder = replyFolder;
            error = null;

            var normalizedMode = NullIfWhiteSpace(mode)?.ToLowerInvariant();
            var needsSource = normalizedMode is "reply" or "forward";
            if (!needsSource || replyUid > 0)
            {
                return true;
            }

            if (!MailboxOpenRequestBuilder.TryResolve(0, listIndex, replyFolder, accountAlias, ListFor(accountAlias), out var openRequest, out error))
            {
                return false;
            }

            uid = openRequest!.Message.Uid;
            folder = openRequest.Message.Folder ?? replyFolder;
            return true;
        }

        private async Task<string> RunMailboxAsync<TResult>(
            string? mailboxAlias,
            Func<MailboxAccountContext, Task<Result<TResult>>> invoke,
            Func<TResult, string> format,
            CancellationToken cancellationToken)
            where TResult : class
        {
            var (account, error) = await GetAccountAsync(mailboxAlias, cancellationToken);
            if (error is not null)
            {
                return error;
            }

            return await RunMailboxAsync(account!, invoke, format);
        }

        private static async Task<string> RunMailboxAsync<TResult>(
            MailboxAccountContext account,
            Func<MailboxAccountContext, Task<Result<TResult>>> invoke,
            Func<TResult, string> format)
            where TResult : class
        {
            var outcome = await invoke(account);
            if (outcome.HasError)
            {
                return outcome.FirstErrorMessage!;
            }

            return WithAccountHeader(account, format(outcome.Payload!));
        }

        #endregion
    }
}

internal sealed record EmailTriageToolRun(IList<AITool> Tools, Func<CancellationToken, Task> PersistAsync);
