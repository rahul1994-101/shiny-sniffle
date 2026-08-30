using Application.Features.Shared;

namespace Application.Features.Workspace.EmailAccounts;

/// <summary>
/// Resolves a user's workspace mailbox account (default, alias, or <c>mailbox:alias</c>) to runtime IMAP/SMTP settings.
/// </summary>
public sealed class MailboxAccountResolver(EmailAccountRepository emailAccountRepo)
{
    public async Task<bool> IsConfiguredAsync(Guid userId, string? mailboxRef = null, CancellationToken cancellationToken = default)
    {
        var outcome = await TryResolveAccountAsync(userId, mailboxRef, cancellationToken);
        return outcome.IsSuccess;
    }

    public async Task<MailboxResult<MailboxAccountContext>> TryResolveAccountAsync(Guid userId, string? mailboxRef = null, CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveAsync(userId, mailboxRef, cancellationToken);
        if (resolved.Context is null)
        {
            return MailboxResult<MailboxAccountContext>.Fail(resolved.ErrorMessage!);
        }

        return MailboxResult<MailboxAccountContext>.Ok(resolved.Context, resolved.Context);
    }

    internal async Task<MailboxAccountResolveResult> ResolveAsync(
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
                ? MailboxAccountResolveResult.Fail(MailboxMessages.AccountNotFound(alias))
                : MailboxAccountResolveResult.Fail(MailboxMessages.NotConfigured);
        }

        if (account.EmailProvider is null)
        {
            return MailboxAccountResolveResult.Fail(MailboxMessages.NotConfigured);
        }

        var settings = EmailAccountMapping.ToStoredSettings(account, account.EmailProvider);
        var runtime = EmailSettingsMapping.ToMailRuntime(settings);
        if (runtime is null)
        {
            var message = alias is not null
                ? MailboxMessages.IncompleteAccount(account.EmailAddress)
                : MailboxMessages.NotConfigured;

            return MailboxAccountResolveResult.Fail(message);
        }

        return MailboxAccountResolveResult.Ok(new MailboxAccountContext
        {
            EmailAccountId = account.Id,
            Alias = account.Alias,
            EmailAddress = account.EmailAddress,
            ProviderName = account.EmailProvider.Name,
            IsDefault = account.IsDefault,
            Runtime = runtime
        });
    }
}
