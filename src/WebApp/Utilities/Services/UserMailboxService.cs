using System.Net.Mail;

using WebApp.Features.UserSettings;
using WebApp.Utilities.Helpers;

namespace WebApp.Utilities.Services;

public sealed class UserMailboxService(UserSettingsRepository userSettingsRepo, IMailboxService mailboxService)
{
    public async Task<bool> IsConfiguredAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var emailSettings = await userSettingsRepo.GetUserEmailSettingsAsync(userId, cancellationToken);
        return EmailSettingsMapping.IsMailboxConfigured(emailSettings);
    }

    public async Task<MailboxStatusResult> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var config = await ResolveMailRuntimeAsync(userId, cancellationToken: cancellationToken);
        if (config is null)
        {
            return new MailboxStatusResult
            {
                IsConfigured = false,
                IsReachable = false,
                Message = EmailReadConstants.NotConfiguredForAgent
            };
        }

        return await mailboxService.GetStatusAsync(config, cancellationToken);
    }

    public async Task<InboxListResult> ListInboxAsync(Guid userId, InboxQuery query, CancellationToken cancellationToken = default)
    {
        var config = await ResolveMailRuntimeAsync(userId, cancellationToken: cancellationToken);
        if (config is null)
        {
            return new InboxListResult();
        }

        return await mailboxService.ListInboxAsync(config, query, cancellationToken);
    }

    public async Task<InboxMessageDetail?> GetInboxMessageAsync(Guid userId, uint uid, string? folder = null, CancellationToken cancellationToken = default)
    {
        var config = await ResolveMailRuntimeAsync(userId, cancellationToken: cancellationToken);
        if (config is null)
        {
            return null;
        }

        return await mailboxService.GetInboxMessageAsync(config, uid, folder, cancellationToken);
    }

    public async Task<IReadOnlyList<MailboxFolderInfo>> ListFoldersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var config = await ResolveMailRuntimeAsync(userId, cancellationToken: cancellationToken);
        if (config is null)
        {
            return [];
        }

        return await mailboxService.ListFoldersAsync(config, cancellationToken);
    }

    public async Task<SendMailResult> SendAsync(Guid userId, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        if (!MailAddress.TryCreate(mail.To, out _))
        {
            return new SendMailResult
            {
                Success = false,
                Message = "Recipient email address is invalid."
            };
        }

        var config = await ResolveMailRuntimeAsync(userId, cancellationToken: cancellationToken);
        if (config is null)
        {
            return new SendMailResult
            {
                Success = false,
                Message = EmailReadConstants.NotConfiguredForSend
            };
        }

        return await mailboxService.SendAsync(config, mail, cancellationToken);
    }

    public async Task<MailboxTestResult> TestConnectionAsync(
        Guid userId,
        EmailSettingsDto? draft = null,
        CancellationToken cancellationToken = default)
    {
        var config = await ResolveMailRuntimeAsync(userId, draft, cancellationToken);
        if (config is null)
        {
            return new MailboxTestResult
            {
                Success = false,
                Message = "Complete mailbox settings (including password) before testing the connection."
            };
        }

        return await mailboxService.TestConnectionAsync(config, cancellationToken);
    }

    #region # Private Helpers

    private async Task<EmailSettings?> ResolveMailRuntimeAsync(
        Guid userId,
        EmailSettingsDto? draft = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stored = await userSettingsRepo.GetUserEmailSettingsAsync(userId, cancellationToken);
        var resolved = EmailSettingsMapping.ResolveForMail(stored, draft);
        return EmailSettingsMapping.ToMailRuntime(resolved);
    }

    #endregion
}
