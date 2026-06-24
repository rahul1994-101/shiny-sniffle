using System.Net.Mail;

using WebApp.Data;
using WebApp.Models;
using WebApp.Utilities.Extensions;
using WebApp.Utilities.Helpers;

namespace WebApp.Utilities.Services;

public sealed class UserMailboxService(Persistence _repo, IMailboxService _mailboxService)
{
    public async Task<bool> IsConfiguredAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var emailSettings = await _repo.GetUserEmailSettingsAsync(userId);
        return emailSettings.IsMailboxConfigured();
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
                Message = EmailMailboxTextHelpers.NotConfiguredForAgent
            };
        }

        return await _mailboxService.GetStatusAsync(config, cancellationToken);
    }

    public async Task<IReadOnlyList<InboxMessageSummary>> ListInboxAsync(Guid userId, InboxQuery query, CancellationToken cancellationToken = default)
    {
        var config = await ResolveMailRuntimeAsync(userId, cancellationToken: cancellationToken);
        if (config is null)
        {
            return [];
        }

        return await _mailboxService.ListInboxAsync(config, query, cancellationToken);
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
                Message = EmailMailboxTextHelpers.NotConfiguredForSend
            };
        }

        return await _mailboxService.SendAsync(config, mail, cancellationToken);
    }

    public async Task<MailboxTestResult> TestConnectionAsync(Guid userId, EmailSettingsDto? draft = null, CancellationToken cancellationToken = default)
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

        return await _mailboxService.TestConnectionAsync(config, cancellationToken);
    }

    #region # Private Helpers

    private async Task<EmailSettings?> ResolveMailRuntimeAsync(Guid userId, EmailSettingsDto? draft = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stored = await _repo.GetUserEmailSettingsAsync(userId);
        var resolved = EmailSettingsHelpers.ResolveForMail(stored, draft);
        return resolved.ToMailRuntime();
    }

    #endregion
}
