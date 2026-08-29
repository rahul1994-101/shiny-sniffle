using Application.Features.Shared;
using Infrastructure.Mailbox;

namespace Application.Features.Workspace.EmailAccounts;

/// <summary>
/// Resolves a user's workspace mailbox account (default, alias, or <c>mailbox:alias</c>) to runtime IMAP/SMTP settings.
/// </summary>
public sealed class MailboxAccountResolver(EmailAccountRepository emailAccountRepo)
{
    public async Task<MailboxAccountResolveResult> ResolveAsync(
        Guid userId,
        string? mailboxRef = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? alias = null;
        if (!string.IsNullOrWhiteSpace(mailboxRef))
        {
            var trimmed = mailboxRef.Trim();
            if (EntityRefs.TryParse(trimmed, out var kind, out var parsedAlias))
            {
                if (kind != EntityRefs.Kind.Mailbox)
                {
                    return MailboxAccountResolveResult.Fail(
                        $"Expected a mailbox reference (mailbox:alias), got \"{trimmed}\".");
                }

                alias = parsedAlias;
            }
            else
            {
                alias = EmailAccountMapping.NormalizeAlias(trimmed) ?? trimmed;
            }
        }

        var account = await emailAccountRepo.GetActiveAccountAsync(userId, alias, cancellationToken);
        if (account is null)
        {
            return alias is not null
                ? MailboxAccountResolveResult.Fail(EmailReadConstants.FormatMailboxNotFound(alias))
                : MailboxAccountResolveResult.Fail(EmailReadConstants.NotConfiguredForAgent);
        }

        if (account.EmailProvider is null)
        {
            return MailboxAccountResolveResult.Fail(EmailReadConstants.NotConfiguredForAgent);
        }

        var settings = EmailAccountMapping.ToStoredSettings(account, account.EmailProvider);
        var runtime = EmailSettingsMapping.ToMailRuntime(settings);
        if (runtime is null)
        {
            var message = alias is not null
                ? $"Mailbox {account.EmailAddress} is incomplete. Finish setup in Workspace → Email accounts."
                : EmailReadConstants.NotConfiguredForAgent;

            return MailboxAccountResolveResult.Fail(message);
        }

        return MailboxAccountResolveResult.Ok(new MailboxAccountContext
        {
            EmailAccountId = account.Id,
            Alias = account.Alias,
            EmailAddress = account.EmailAddress,
            IsDefault = account.IsDefault,
            Runtime = runtime
        });
    }
}
