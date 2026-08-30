using Application.Features.Workspace.EmailAccounts;
using Infrastructure.Mailbox;

namespace Application.Features.Shared;

/// <summary>
/// Consumer-agnostic Application gateway to <see cref="IMailboxService"/> for a resolved <see cref="MailboxAccountContext"/>.
/// Account resolution belongs upstream — see <see cref="WorkspaceReferenceService"/>.
/// </summary>
public sealed class WorkspaceMailboxService(EmailAccountRepository emailAccountRepo, IMailboxService mailboxService)
{
    #region # Setup

    public async Task<TestConnectionResult> TestConnectionWithDraftAsync(Guid userId, EmailSettingsDto? draft = null, CancellationToken cancellationToken = default)
    {
        var stored = await emailAccountRepo.GetDefaultStoredMailboxSettingsAsync(userId, cancellationToken);
        var resolved = EmailSettingsMapping.ResolveForMail(stored, draft);
        var runtime = EmailSettingsMapping.ToMailRuntime(resolved);
        if (runtime is null)
        {
            return new TestConnectionResult
            {
                Message = "Complete mailbox settings (including password) before testing the connection."
            };
        }

        return await mailboxService.TestConnectionAsync(runtime, cancellationToken);
    }

    #endregion

    #region # Queries

    public Task<MailboxResult<ListMessagesResult>> ListMessagesAsync(MailboxAccountContext account, ListMessagesFilters filters, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(account, ListMessages, "Could not list messages", cancellationToken);

        Task<ListMessagesResult> ListMessages(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.ListMessagesAsync(resolvedAccount.Runtime, filters, ct);
        }
    }

    public Task<MailboxResult<MessageDetail>> GetMessageAsync(MailboxAccountContext account, MessageKey message, CancellationToken cancellationToken = default)
    {
        return GetMessageCoreAsync(account, message, cancellationToken);
    }

    public Task<MailboxResult<GetMessagesResult>> GetMessagesAsync(MailboxAccountContext account, MessageBatchFilters filters, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(account, GetMessages, "Could not read messages", cancellationToken);

        Task<GetMessagesResult> GetMessages(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.GetMessagesAsync(resolvedAccount.Runtime, filters, ct);
        }
    }

    public Task<MailboxResult<ListFoldersResult>> ListFoldersAsync(MailboxAccountContext account, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(account, ListFolders, "Could not list mailbox folders", cancellationToken);

        Task<ListFoldersResult> ListFolders(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.ListFoldersAsync(resolvedAccount.Runtime, ct);
        }
    }

    public Task<MailboxResult<GetAttachmentsResult>> GetAttachmentsAsync(MailboxAccountContext account, GetAttachmentsFilters filters, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(account, GetAttachments, "Could not fetch attachments", cancellationToken);

        Task<GetAttachmentsResult> GetAttachments(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.GetAttachmentsAsync(resolvedAccount.Runtime, filters, ct);
        }
    }

    public Task<MailboxResult<GetFolderResult>> GetFolderAsync(MailboxAccountContext account, GetFolderFilters filters, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(account, GetFolder, "Could not read folder stats", cancellationToken);

        Task<GetFolderResult> GetFolder(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.GetFolderAsync(resolvedAccount.Runtime, filters, ct);
        }
    }

    public Task<MailboxResult<TestConnectionResult>> TestConnectionAsync(MailboxAccountContext account, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(account, GetStatus, "Could not check mailbox status", cancellationToken);

        Task<TestConnectionResult> GetStatus(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.TestConnectionAsync(resolvedAccount.Runtime, ct);
        }
    }

    #endregion

    #region # Commands

    public Task<MailboxResult<CommandResult>> DeleteMessagesAsync(MailboxAccountContext account, MessageBatchFilters filters, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(account, DeleteMessages, "Could not delete messages", cancellationToken);

        Task<CommandResult> DeleteMessages(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.DeleteMessagesAsync(resolvedAccount.Runtime, filters, ct);
        }
    }

    public Task<MailboxResult<CommandResult>> MoveMessagesAsync(MailboxAccountContext account, MessageTransferFilters filters, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(account, MoveMessages, "Could not move messages", cancellationToken);

        Task<CommandResult> MoveMessages(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.MoveMessagesAsync(resolvedAccount.Runtime, filters, ct);
        }
    }

    public Task<MailboxResult<CommandResult>> SetMessageFlagsAsync(MailboxAccountContext account, SetMessageFlagsFilters filters, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(account, SetMessageFlags, "Could not update message flags", cancellationToken);

        Task<CommandResult> SetMessageFlags(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.SetMessageFlagsAsync(resolvedAccount.Runtime, filters, ct);
        }
    }

    public Task<MailboxResult<CommandResult>> CopyMessagesAsync(MailboxAccountContext account, MessageTransferFilters filters, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(account, CopyMessages, "Could not copy messages", cancellationToken);

        Task<CommandResult> CopyMessages(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.CopyMessagesAsync(resolvedAccount.Runtime, filters, ct);
        }
    }

    public Task<MailboxResult<CommandResult>> CreateFolderAsync(MailboxAccountContext account, CreateFolderFilters filters, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(account, CreateFolder, "Could not create folder", cancellationToken);

        Task<CommandResult> CreateFolder(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.CreateFolderAsync(resolvedAccount.Runtime, filters, ct);
        }
    }

    public Task<MailboxResult<SendMailResult>> SendAsync(MailboxAccountContext account, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        return ExecuteWithSuccessResultAsync(account, SendMail, "Could not send email", SendMailError, cancellationToken);

        Task<SendMailResult> SendMail(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.SendAsync(resolvedAccount.Runtime, mail, ct);
        }

        static string? SendMailError(SendMailResult result)
        {
            return result.Success ? null : result.Message;
        }
    }

    public Task<MailboxResult<SaveDraftResult>> SaveDraftAsync(MailboxAccountContext account, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        return ExecuteWithSuccessResultAsync(account, SaveDraft, "Could not save draft", SaveDraftError, cancellationToken);

        Task<SaveDraftResult> SaveDraft(MailboxAccountContext resolvedAccount, CancellationToken ct)
        {
            return mailboxService.SaveDraftAsync(resolvedAccount.Runtime, mail, ct);
        }

        static string? SaveDraftError(SaveDraftResult result)
        {
            return result.Success ? null : result.Message;
        }
    }

    #endregion

    #region # Orchestration

    private async Task<MailboxResult<T>> ExecuteAsync<T>(MailboxAccountContext account, Func<MailboxAccountContext, CancellationToken, Task<T>> execute, string failurePrefix, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var result = await execute(account, cancellationToken);
            return MailboxResult<T>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<T>.Fail($"{failurePrefix}: {ex.Message}");
        }
    }

    private async Task<MailboxResult<T>> ExecuteWithSuccessResultAsync<T>(MailboxAccountContext account, Func<MailboxAccountContext, CancellationToken, Task<T>> execute, string failurePrefix, Func<T, string?> resultError, CancellationToken cancellationToken)
        where T : class
    {
        var outcome = await ExecuteAsync(account, execute, failurePrefix, cancellationToken);
        if (!outcome.IsSuccess || outcome.Value is null)
        {
            return outcome;
        }

        var message = resultError(outcome.Value);
        if (message is null)
        {
            return outcome;
        }

        return MailboxResult<T>.Fail(message);
    }

    private async Task<MailboxResult<MessageDetail>> GetMessageCoreAsync(MailboxAccountContext account, MessageKey message, CancellationToken cancellationToken)
    {
        try
        {
            var result = await mailboxService.GetMessagesAsync(account.Runtime, new MessageBatchFilters { Messages = [message] }, cancellationToken);
            var detail = result.Messages.Count == 0 ? null : result.Messages[0];
            if (detail is not null)
            {
                return MailboxResult<MessageDetail>.Ok(account, detail);
            }

            var folderLabel = string.IsNullOrWhiteSpace(message.Folder)
                ? "inbox"
                : message.Folder.Trim();
            return MailboxResult<MessageDetail>.Fail($"No message found with Uid {message.Uid} in folder '{folderLabel}'.");
        }
        catch (Exception ex)
        {
            return MailboxResult<MessageDetail>.Fail($"Could not read message: {ex.Message}");
        }
    }

    #endregion
}
