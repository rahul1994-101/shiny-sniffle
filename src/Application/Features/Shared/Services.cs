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

    public async Task<Result<ListMessagesResult>> ListMessagesAsync(MailboxAccountContext account, ListMessagesFilters filters, CancellationToken cancellationToken = default)
    {
        var result = new Result<ListMessagesResult>();
        try
        {
            var listResult = await mailboxService.ListMessagesAsync(account.Runtime, filters, cancellationToken);
            result.Success(listResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not list messages: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<MessageDetail>> GetMessageAsync(MailboxAccountContext account, MessageKey message, CancellationToken cancellationToken = default)
    {
        var result = new Result<MessageDetail>();
        try
        {
            var getResult = await mailboxService.GetMessagesAsync(account.Runtime, new MessageBatchFilters { Messages = [message] }, cancellationToken);
            var detail = getResult.Messages.Count == 0 ? null : getResult.Messages[0];
            if (detail is not null)
            {
                result.Success(detail);
                return result;
            }

            var folderLabel = string.IsNullOrWhiteSpace(message.Folder)
                ? "inbox"
                : message.Folder.Trim();
            result.Failure(ErrorCode.NotFound, $"No message found with Uid {message.Uid} in folder '{folderLabel}'.");
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not read message: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<GetMessagesResult>> GetMessagesAsync(MailboxAccountContext account, MessageBatchFilters filters, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetMessagesResult>();
        try
        {
            var getResult = await mailboxService.GetMessagesAsync(account.Runtime, filters, cancellationToken);
            result.Success(getResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not read messages: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<ListFoldersResult>> ListFoldersAsync(MailboxAccountContext account, CancellationToken cancellationToken = default)
    {
        var result = new Result<ListFoldersResult>();
        try
        {
            var listResult = await mailboxService.ListFoldersAsync(account.Runtime, cancellationToken);
            result.Success(listResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not list mailbox folders: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<GetAttachmentsResult>> GetAttachmentsAsync(MailboxAccountContext account, GetAttachmentsFilters filters, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetAttachmentsResult>();
        try
        {
            var attachmentResult = await mailboxService.GetAttachmentsAsync(account.Runtime, filters, cancellationToken);
            result.Success(attachmentResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not fetch attachments: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<GetFolderResult>> GetFolderAsync(MailboxAccountContext account, GetFolderFilters filters, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetFolderResult>();
        try
        {
            var folderResult = await mailboxService.GetFolderAsync(account.Runtime, filters, cancellationToken);
            result.Success(folderResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not read folder stats: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<TestConnectionResult>> TestConnectionAsync(MailboxAccountContext account, CancellationToken cancellationToken = default)
    {
        var result = new Result<TestConnectionResult>();
        try
        {
            var testResult = await mailboxService.TestConnectionAsync(account.Runtime, cancellationToken);
            result.Success(testResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not check mailbox status: {ex.Message}");
        }

        return result;
    }

    #endregion

    #region # Commands

    public async Task<Result<CommandResult>> DeleteMessagesAsync(MailboxAccountContext account, MessageBatchFilters filters, CancellationToken cancellationToken = default)
    {
        var result = new Result<CommandResult>();
        try
        {
            var commandResult = await mailboxService.DeleteMessagesAsync(account.Runtime, filters, cancellationToken);
            result.Success(commandResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not delete messages: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<CommandResult>> MoveMessagesAsync(MailboxAccountContext account, MessageTransferFilters filters, CancellationToken cancellationToken = default)
    {
        var result = new Result<CommandResult>();
        try
        {
            var commandResult = await mailboxService.MoveMessagesAsync(account.Runtime, filters, cancellationToken);
            result.Success(commandResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not move messages: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<CommandResult>> SetMessageFlagsAsync(MailboxAccountContext account, SetMessageFlagsFilters filters, CancellationToken cancellationToken = default)
    {
        var result = new Result<CommandResult>();
        try
        {
            var commandResult = await mailboxService.SetMessageFlagsAsync(account.Runtime, filters, cancellationToken);
            result.Success(commandResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not update message flags: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<CommandResult>> CopyMessagesAsync(MailboxAccountContext account, MessageTransferFilters filters, CancellationToken cancellationToken = default)
    {
        var result = new Result<CommandResult>();
        try
        {
            var commandResult = await mailboxService.CopyMessagesAsync(account.Runtime, filters, cancellationToken);
            result.Success(commandResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not copy messages: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<CommandResult>> CreateFolderAsync(MailboxAccountContext account, CreateFolderFilters filters, CancellationToken cancellationToken = default)
    {
        var result = new Result<CommandResult>();
        try
        {
            var commandResult = await mailboxService.CreateFolderAsync(account.Runtime, filters, cancellationToken);
            result.Success(commandResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not create folder: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<SendMailResult>> SendAsync(MailboxAccountContext account, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        var result = new Result<SendMailResult>();
        try
        {
            var sendResult = await mailboxService.SendAsync(account.Runtime, mail, cancellationToken);
            if (!sendResult.Success)
            {
                result.Failure(ErrorCode.BadRequest, sendResult.Message);
                return result;
            }

            result.Success(sendResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not send email: {ex.Message}");
        }

        return result;
    }

    public async Task<Result<SaveDraftResult>> SaveDraftAsync(MailboxAccountContext account, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        var result = new Result<SaveDraftResult>();
        try
        {
            var saveResult = await mailboxService.SaveDraftAsync(account.Runtime, mail, cancellationToken);
            if (!saveResult.Success)
            {
                result.Failure(ErrorCode.BadRequest, saveResult.Message);
                return result;
            }

            result.Success(saveResult);
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.BadRequest, $"Could not save draft: {ex.Message}");
        }

        return result;
    }

    #endregion
}
