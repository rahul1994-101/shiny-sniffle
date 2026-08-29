using Application.Features.Workspace.EmailAccounts;
using Infrastructure.Mailbox;

namespace Application.Features.Shared;

public sealed class UserMailboxService(
    MailboxAccountResolver mailboxAccountResolver,
    EmailAccountRepository emailAccountRepo,
    IMailboxService mailboxService)
{
    #region # Account resolution

    public Task<MailboxAccountResolveResult> ResolveAccountAsync(
        Guid userId,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default) =>
        mailboxAccountResolver.ResolveAsync(userId, mailboxRef, cancellationToken);

    public async Task<bool> IsConfiguredAsync(
        Guid userId,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default)
    {
        var resolved = await mailboxAccountResolver.ResolveAsync(userId, mailboxRef, cancellationToken);
        return resolved.IsSuccess;
    }

    #endregion

    #region # Queries

    public async Task<MailboxStatusResult> GetStatusAsync(
        Guid userId,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default)
    {
        var resolved = await mailboxAccountResolver.ResolveAsync(userId, mailboxRef, cancellationToken);
        if (resolved.Context is null)
        {
            return new MailboxStatusResult
            {
                IsConfigured = false,
                IsReachable = false,
                Message = resolved.ErrorMessage ?? EmailReadConstants.NotConfiguredForAgent
            };
        }

        return await GetStatusAsync(resolved.Context, cancellationToken);
    }

    internal Task<MailboxStatusResult> GetStatusAsync(
        MailboxAccountContext context,
        CancellationToken cancellationToken = default) =>
        mailboxService.GetStatusAsync(context.Runtime, cancellationToken);

    internal Task<InboxListResult> ListMessagesAsync(
        MailboxAccountContext context,
        InboxListRequest request,
        CancellationToken cancellationToken = default) =>
        mailboxService.ListMessagesAsync(context.Runtime, request.ToInboxQuery(), cancellationToken);

    internal Task<InboxMessageDetail?> GetMessageAsync(
        MailboxAccountContext context,
        InboxOpenRequest request,
        CancellationToken cancellationToken = default) =>
        mailboxService.GetMessageAsync(context.Runtime, request.Message, cancellationToken);

    internal Task<IReadOnlyList<InboxMessageDetail>> GetMessagesAsync(
        MailboxAccountContext context,
        MailboxMessageBatchRequest request,
        CancellationToken cancellationToken = default) =>
        mailboxService.GetMessagesAsync(context.Runtime, request.Messages, cancellationToken);

    internal Task<IReadOnlyList<MailboxFolderInfo>> ListFoldersAsync(
        MailboxAccountContext context,
        CancellationToken cancellationToken = default) =>
        mailboxService.ListFoldersAsync(context.Runtime, cancellationToken);

    #endregion

    #region # Commands

    internal Task<SendMailResult> SendAsync(
        MailboxAccountContext context,
        SendMailRequest request,
        CancellationToken cancellationToken = default) =>
        mailboxService.SendAsync(context.Runtime, request.Mail, cancellationToken);

    internal Task<MailboxCommandResult> DeleteMessagesAsync(
        MailboxAccountContext context,
        MailboxMessageBatchRequest request,
        CancellationToken cancellationToken = default) =>
        mailboxService.DeleteMessagesAsync(context.Runtime, request.Messages, cancellationToken);

    internal Task<MailboxCommandResult> MoveMessagesAsync(
        MailboxAccountContext context,
        MailboxMoveRequest request,
        CancellationToken cancellationToken = default) =>
        mailboxService.MoveMessagesAsync(
            context.Runtime, request.Batch.Messages, request.DestinationFolder, cancellationToken);

    internal Task<MailboxCommandResult> SetMessageFlagsAsync(
        MailboxAccountContext context,
        MailboxFlagRequest request,
        CancellationToken cancellationToken = default) =>
        mailboxService.SetMessageFlagsAsync(context.Runtime, request.Batch.Messages, request.Flag, cancellationToken);

    #endregion

    #region # Connection test

    public async Task<MailboxTestResult> TestConnectionAsync(
        Guid userId,
        EmailSettingsDto? draft = null,
        CancellationToken cancellationToken = default)
    {
        var stored = await emailAccountRepo.GetDefaultEmailSettingsAsync(userId, cancellationToken);
        var resolved = EmailSettingsMapping.ResolveForMail(stored, draft);
        var runtime = EmailSettingsMapping.ToMailRuntime(resolved);
        if (runtime is null)
        {
            return new MailboxTestResult
            {
                Success = false,
                Message = "Complete mailbox settings (including password) before testing the connection."
            };
        }

        return await mailboxService.TestConnectionAsync(runtime, cancellationToken);
    }

    #endregion
}

/// <summary>Agent-facing mailbox operations — account resolution + service calls; tools format output.</summary>
public sealed class MailboxAgentService(UserMailboxService mailboxService)
{
    internal Task<(MailboxAccountContext? Account, InboxListResult? Result, string? Error)> ListInboxAsync(
        Guid userId,
        InboxListRequest request,
        string? mailboxRef,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            userId,
            mailboxRef,
            EmailReadConstants.NotConfiguredForList,
            (account, ct) => mailboxService.ListMessagesAsync(account, request, ct),
            "Could not list messages",
            cancellationToken);

    internal async Task<(MailboxAccountContext? Account, InboxMessageDetail? Message, string? Error)> OpenInboxAsync(
        Guid userId,
        InboxOpenRequest request,
        string? mailboxRef,
        CancellationToken cancellationToken = default)
    {
        var (account, error) = await RequireAccountAsync(
            userId, mailboxRef, EmailReadConstants.NotConfiguredForGet, cancellationToken);
        if (error is not null)
        {
            return (null, null, error);
        }

        try
        {
            var message = await mailboxService.GetMessageAsync(account!, request, cancellationToken);
            if (message is not null)
            {
                return (account, message, null);
            }

            var folderLabel = string.IsNullOrWhiteSpace(request.Message.Folder)
                ? "inbox"
                : request.Message.Folder.Trim();
            return (account, null, $"No message found with Uid {request.Message.Uid} in folder '{folderLabel}'.");
        }
        catch (Exception ex)
        {
            return (null, null, $"Could not read message: {ex.Message}");
        }
    }

    internal async Task<(MailboxAccountContext? Account, IReadOnlyList<InboxMessageDetail>? Messages, string? Error)> OpenInboxBatchAsync(
        Guid userId,
        MailboxMessageBatchRequest request,
        string? mailboxRef,
        CancellationToken cancellationToken = default)
    {
        if (request.Messages.Count > MailboxReadLimits.MaxBatchGetCount)
        {
            return (
                null,
                null,
                $"At most {MailboxReadLimits.MaxBatchGetCount} Uids per call. Split into multiple get_inbox_messages calls or use get_inbox_message for one message.");
        }

        return await ExecuteAsync(
            userId,
            mailboxRef,
            EmailReadConstants.NotConfiguredForGet,
            (account, ct) => mailboxService.GetMessagesAsync(account, request, ct),
            "Could not read messages",
            cancellationToken);
    }

    internal Task<(MailboxAccountContext? Account, IReadOnlyList<MailboxFolderInfo>? Folders, string? Error)> ListFoldersAsync(
        Guid userId,
        string? mailboxRef,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            userId,
            mailboxRef,
            EmailReadConstants.NotConfiguredForFolders,
            (account, ct) => mailboxService.ListFoldersAsync(account, ct),
            "Could not list mailbox folders",
            cancellationToken);

    internal async Task<(MailboxAccountContext? Account, MailboxStatusResult? Status, string? Error)> GetStatusAsync(
        Guid userId,
        string? mailboxRef,
        CancellationToken cancellationToken = default)
    {
        var (account, error) = await RequireAccountAsync(
            userId, mailboxRef, EmailReadConstants.NotConfiguredForAgent, cancellationToken);
        if (error is not null)
        {
            return (null, null, error);
        }

        var status = await mailboxService.GetStatusAsync(account!, cancellationToken);
        return (account, status, null);
    }

    internal Task<(MailboxAccountContext? Account, MailboxCommandResult? Result, string? Error)> DeleteMessagesAsync(
        Guid userId,
        MailboxMessageBatchRequest request,
        string? mailboxRef,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            userId,
            mailboxRef,
            EmailReadConstants.NotConfiguredForCommands,
            (account, ct) => mailboxService.DeleteMessagesAsync(account, request, ct),
            "Could not delete messages",
            cancellationToken);

    internal Task<(MailboxAccountContext? Account, MailboxCommandResult? Result, string? Error)> MoveMessagesAsync(
        Guid userId,
        MailboxMoveRequest request,
        string? mailboxRef,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            userId,
            mailboxRef,
            EmailReadConstants.NotConfiguredForCommands,
            (account, ct) => mailboxService.MoveMessagesAsync(account, request, ct),
            "Could not move messages",
            cancellationToken);

    internal Task<(MailboxAccountContext? Account, MailboxCommandResult? Result, string? Error)> SetMessageFlagsAsync(
        Guid userId,
        MailboxFlagRequest request,
        string? mailboxRef,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            userId,
            mailboxRef,
            EmailReadConstants.NotConfiguredForCommands,
            (account, ct) => mailboxService.SetMessageFlagsAsync(account, request, ct),
            "Could not update message flags",
            cancellationToken);

    internal async Task<(MailboxAccountContext? Account, SendMailResult? Result, string? Error)> SendAsync(
        Guid userId,
        SendMailRequest request,
        string? mailboxRef,
        CancellationToken cancellationToken = default)
    {
        var (account, error) = await RequireAccountAsync(
            userId, mailboxRef, EmailReadConstants.NotConfiguredForSend, cancellationToken);
        if (error is not null)
        {
            return (null, null, error);
        }

        try
        {
            var result = await mailboxService.SendAsync(account!, request, cancellationToken);
            return result.Success
                ? (account, result, null)
                : (account, result, result.Message);
        }
        catch (Exception ex)
        {
            return (null, null, $"Could not send email: {ex.Message}");
        }
    }

    private async Task<(MailboxAccountContext? Account, T? Result, string? Error)> ExecuteAsync<T>(
        Guid userId,
        string? mailboxRef,
        string notConfiguredMessage,
        Func<MailboxAccountContext, CancellationToken, Task<T>> execute,
        string failurePrefix,
        CancellationToken cancellationToken)
        where T : class
    {
        var (account, error) = await RequireAccountAsync(userId, mailboxRef, notConfiguredMessage, cancellationToken);
        if (error is not null)
        {
            return (null, null, error);
        }

        try
        {
            var result = await execute(account!, cancellationToken);
            return (account, result, null);
        }
        catch (Exception ex)
        {
            return (null, null, $"{failurePrefix}: {ex.Message}");
        }
    }

    private async Task<(MailboxAccountContext? Account, string? Error)> RequireAccountAsync(
        Guid userId,
        string? mailboxRef,
        string notConfiguredMessage,
        CancellationToken cancellationToken)
    {
        var resolved = await mailboxService.ResolveAccountAsync(userId, mailboxRef, cancellationToken);
        if (resolved.Context is null)
        {
            return (null, resolved.ErrorMessage ?? notConfiguredMessage);
        }

        return (resolved.Context, null);
    }
}
