using Application.Features.Workspace.EmailAccounts;
using Infrastructure.Mailbox;

namespace Application.Features.Shared;

/// <summary>
/// Consumer-agnostic Application gateway to <see cref="IMailboxService"/> for a resolved <see cref="MailboxAccountContext"/>.
/// Account resolution belongs upstream — see <see cref="WorkspaceReferenceService"/>.
/// </summary>
public sealed class WorkspaceMailboxService(IMailboxService mailboxService)
{
    #region # Queries

    public async Task<MailboxResult<ListMessagesResult>> ListMessagesAsync(MailboxAccountContext account, ListMessagesFilters filters, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.ListMessagesAsync(account.Runtime, filters, cancellationToken);
            return MailboxResult<ListMessagesResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<ListMessagesResult>.Fail($"Could not list messages: {ex.Message}");
        }
    }

    public async Task<MailboxResult<MessageDetail>> GetMessageAsync(MailboxAccountContext account, MessageKey message, CancellationToken cancellationToken = default)
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

    public async Task<MailboxResult<GetMessagesResult>> GetMessagesAsync(MailboxAccountContext account, MessageBatchFilters filters, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.GetMessagesAsync(account.Runtime, filters, cancellationToken);
            return MailboxResult<GetMessagesResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<GetMessagesResult>.Fail($"Could not read messages: {ex.Message}");
        }
    }

    public async Task<MailboxResult<ListFoldersResult>> ListFoldersAsync(MailboxAccountContext account, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.ListFoldersAsync(account.Runtime, cancellationToken);
            return MailboxResult<ListFoldersResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<ListFoldersResult>.Fail($"Could not list mailbox folders: {ex.Message}");
        }
    }

    public async Task<MailboxResult<GetAttachmentsResult>> GetAttachmentsAsync(MailboxAccountContext account, GetAttachmentsFilters filters, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.GetAttachmentsAsync(account.Runtime, filters, cancellationToken);
            return MailboxResult<GetAttachmentsResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<GetAttachmentsResult>.Fail($"Could not fetch attachments: {ex.Message}");
        }
    }

    public async Task<MailboxResult<GetFolderResult>> GetFolderAsync(MailboxAccountContext account, GetFolderFilters filters, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.GetFolderAsync(account.Runtime, filters, cancellationToken);
            return MailboxResult<GetFolderResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<GetFolderResult>.Fail($"Could not read folder stats: {ex.Message}");
        }
    }

    public async Task<MailboxResult<TestConnectionResult>> TestConnectionAsync(MailboxAccountContext account, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.TestConnectionAsync(account.Runtime, cancellationToken);
            return MailboxResult<TestConnectionResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<TestConnectionResult>.Fail($"Could not check mailbox status: {ex.Message}");
        }
    }

    #endregion

    #region # Commands

    public async Task<MailboxResult<CommandResult>> DeleteMessagesAsync(MailboxAccountContext account, MessageBatchFilters filters, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.DeleteMessagesAsync(account.Runtime, filters, cancellationToken);
            return MailboxResult<CommandResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<CommandResult>.Fail($"Could not delete messages: {ex.Message}");
        }
    }

    public async Task<MailboxResult<CommandResult>> MoveMessagesAsync(MailboxAccountContext account, MessageTransferFilters filters, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.MoveMessagesAsync(account.Runtime, filters, cancellationToken);
            return MailboxResult<CommandResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<CommandResult>.Fail($"Could not move messages: {ex.Message}");
        }
    }

    public async Task<MailboxResult<CommandResult>> SetMessageFlagsAsync(MailboxAccountContext account, SetMessageFlagsFilters filters, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.SetMessageFlagsAsync(account.Runtime, filters, cancellationToken);
            return MailboxResult<CommandResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<CommandResult>.Fail($"Could not update message flags: {ex.Message}");
        }
    }

    public async Task<MailboxResult<CommandResult>> CopyMessagesAsync(MailboxAccountContext account, MessageTransferFilters filters, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.CopyMessagesAsync(account.Runtime, filters, cancellationToken);
            return MailboxResult<CommandResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<CommandResult>.Fail($"Could not copy messages: {ex.Message}");
        }
    }

    public async Task<MailboxResult<CommandResult>> CreateFolderAsync(MailboxAccountContext account, CreateFolderFilters filters, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.CreateFolderAsync(account.Runtime, filters, cancellationToken);
            return MailboxResult<CommandResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<CommandResult>.Fail($"Could not create folder: {ex.Message}");
        }
    }

    public async Task<MailboxResult<SendMailResult>> SendAsync(MailboxAccountContext account, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.SendAsync(account.Runtime, mail, cancellationToken);
            if (!result.Success)
            {
                return MailboxResult<SendMailResult>.Fail(result.Message);
            }

            return MailboxResult<SendMailResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<SendMailResult>.Fail($"Could not send email: {ex.Message}");
        }
    }

    public async Task<MailboxResult<SaveDraftResult>> SaveDraftAsync(MailboxAccountContext account, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mailboxService.SaveDraftAsync(account.Runtime, mail, cancellationToken);
            if (!result.Success)
            {
                return MailboxResult<SaveDraftResult>.Fail(result.Message);
            }

            return MailboxResult<SaveDraftResult>.Ok(account, result);
        }
        catch (Exception ex)
        {
            return MailboxResult<SaveDraftResult>.Fail($"Could not save draft: {ex.Message}");
        }
    }

    #endregion
}
