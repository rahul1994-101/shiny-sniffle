using System.Net.Mail;
using Application.Features.Workspace.EmailAccounts;
using Infrastructure.Mailbox;

namespace Application.Features.Shared;

public sealed class UserMailboxService(
    MailboxAccountResolver mailboxAccountResolver,
    EmailAccountRepository emailAccountRepo,
    IMailboxService mailboxService)
{
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

        return await mailboxService.GetStatusAsync(resolved.Context.Runtime, cancellationToken);
    }

    public async Task<InboxListResult> ListMessagesAsync(
        Guid userId,
        InboxQuery query,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default)
    {
        var context = await RequireContextAsync(userId, mailboxRef, EmailReadConstants.NotConfiguredForList, cancellationToken);
        return await mailboxService.ListMessagesAsync(context.Runtime, query, cancellationToken);
    }

    public async Task<InboxMessageDetail?> GetMessageAsync(
        Guid userId,
        uint uid,
        string? folder = null,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default)
    {
        var context = await RequireContextAsync(userId, mailboxRef, EmailReadConstants.NotConfiguredForGet, cancellationToken);
        return await mailboxService.GetMessageAsync(context.Runtime, uid, folder, cancellationToken);
    }

    public async Task<IReadOnlyList<InboxMessageDetail>> GetMessagesAsync(
        Guid userId,
        IReadOnlyList<MessageRef> messages,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default)
    {
        var context = await RequireContextAsync(userId, mailboxRef, EmailReadConstants.NotConfiguredForGet, cancellationToken);
        return await mailboxService.GetMessagesAsync(context.Runtime, messages, cancellationToken);
    }

    public async Task<IReadOnlyList<MailboxFolderInfo>> ListFoldersAsync(
        Guid userId,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default)
    {
        var context = await RequireContextAsync(userId, mailboxRef, EmailReadConstants.NotConfiguredForFolders, cancellationToken);
        return await mailboxService.ListFoldersAsync(context.Runtime, cancellationToken);
    }

    public async Task<SendMailResult> SendAsync(
        Guid userId,
        OutboundMail mail,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default)
    {
        if (!MailAddress.TryCreate(mail.To, out _))
        {
            return new SendMailResult
            {
                Success = false,
                Message = "Recipient email address is invalid."
            };
        }

        var resolved = await mailboxAccountResolver.ResolveAsync(userId, mailboxRef, cancellationToken);
        if (resolved.Context is null)
        {
            return new SendMailResult
            {
                Success = false,
                Message = resolved.ErrorMessage ?? EmailReadConstants.NotConfiguredForSend
            };
        }

        return await mailboxService.SendAsync(resolved.Context.Runtime, mail, cancellationToken);
    }

    public async Task<MailboxCommandResult> DeleteMessagesAsync(
        Guid userId,
        IReadOnlyList<MessageRef> messages,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default)
    {
        var context = await RequireContextAsync(userId, mailboxRef, EmailReadConstants.NotConfiguredForCommands, cancellationToken);
        return await mailboxService.DeleteMessagesAsync(context.Runtime, messages, cancellationToken);
    }

    public async Task<MailboxCommandResult> MoveMessagesAsync(
        Guid userId,
        IReadOnlyList<MessageRef> messages,
        string destinationFolder,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default)
    {
        var context = await RequireContextAsync(userId, mailboxRef, EmailReadConstants.NotConfiguredForCommands, cancellationToken);
        return await mailboxService.MoveMessagesAsync(context.Runtime, messages, destinationFolder, cancellationToken);
    }

    public async Task<MailboxCommandResult> SetMessageFlagsAsync(
        Guid userId,
        IReadOnlyList<MessageRef> messages,
        MessageFlagAction flag,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default)
    {
        var context = await RequireContextAsync(userId, mailboxRef, EmailReadConstants.NotConfiguredForCommands, cancellationToken);
        return await mailboxService.SetMessageFlagsAsync(context.Runtime, messages, flag, cancellationToken);
    }

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

    private async Task<MailboxAccountContext> RequireContextAsync(
        Guid userId,
        string? mailboxRef,
        string notConfiguredMessage,
        CancellationToken cancellationToken)
    {
        var resolved = await mailboxAccountResolver.ResolveAsync(userId, mailboxRef, cancellationToken);
        if (resolved.Context is null)
        {
            throw new InvalidOperationException(resolved.ErrorMessage ?? notConfiguredMessage);
        }

        return resolved.Context;
    }
}
