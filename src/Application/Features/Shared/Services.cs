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

    internal Task<ListMessagesResult> ListMessagesAsync(
        MailboxAccountContext context,
        InboxListRequest request,
        CancellationToken cancellationToken = default) =>
        mailboxService.ListMessagesAsync(context.Runtime, request.ToListMessagesFilters(), cancellationToken);

    internal async Task<MessageDetail?> GetMessageAsync(
        MailboxAccountContext context,
        MessageKey message,
        CancellationToken cancellationToken = default)
    {
        var result = await mailboxService.GetMessagesAsync(context.Runtime, new GetMessagesFilters { Messages = [message] }, cancellationToken);
        return result.Messages.Count == 0 ? null : result.Messages[0];
    }

    internal Task<MessageDetail?> GetMessageAsync(
        MailboxAccountContext context,
        InboxOpenRequest request,
        CancellationToken cancellationToken = default) =>
        GetMessageAsync(context, request.Message, cancellationToken);

    internal async Task<IReadOnlyList<MessageDetail>> GetMessagesAsync(
        MailboxAccountContext context,
        IReadOnlyList<MessageKey> messages,
        CancellationToken cancellationToken = default)
    {
        var result = await mailboxService.GetMessagesAsync(context.Runtime, new GetMessagesFilters { Messages = messages }, cancellationToken);
        return result.Messages;
    }

    internal Task<IReadOnlyList<MessageDetail>> GetMessagesAsync(
        MailboxAccountContext context,
        MailboxMessageBatchRequest request,
        CancellationToken cancellationToken = default) =>
        GetMessagesAsync(context, request.Messages, cancellationToken);

    internal Task<ListFoldersResult> ListFoldersAsync(
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

    internal Task<TestConnectionResult> TestConnectionAsync(
        MailboxAccountContext context,
        CancellationToken cancellationToken = default) =>
        mailboxService.TestConnectionAsync(context.Runtime, cancellationToken);

    public async Task<TestConnectionResult> TestConnectionAsync(
        Guid userId,
        EmailSettingsDto? draft = null,
        CancellationToken cancellationToken = default)
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
}

/// <summary>Agent-facing mailbox operations — account resolution + service calls; tools format output.</summary>
public sealed class MailboxAgentService(UserMailboxService mailboxService)
{
    internal Task<(MailboxAccountContext? Account, ListMessagesResult? Result, string? Error)> ListInboxAsync(
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

    internal async Task<(MailboxAccountContext? Account, MessageDetail? Message, string? Error)> OpenInboxAsync(
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

    internal async Task<(MailboxAccountContext? Account, IReadOnlyList<MessageDetail>? Messages, string? Error)> OpenInboxBatchAsync(
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
            (account, ct) => mailboxService.GetMessagesAsync(account, request.Messages, ct),
            "Could not read messages",
            cancellationToken);
    }

    internal Task<(MailboxAccountContext? Account, ListFoldersResult? Result, string? Error)> ListFoldersAsync(
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

    internal async Task<(MailboxAccountContext? Account, TestConnectionResult? Status, string? Error)> GetStatusAsync(
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

        var status = await mailboxService.TestConnectionAsync(account!, cancellationToken);
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
