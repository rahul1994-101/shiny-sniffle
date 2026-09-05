using Application.Features.Shared;
using MediatR.Results;

namespace Application.Features.Workspace.EmailAccounts;

/// <summary>
/// Resolves a user's workspace mailbox account (default, alias, or <c>mailbox:alias</c>) to runtime IMAP/SMTP settings.
/// </summary>
public sealed class MailboxAccountResolver(EmailAccountRepository emailAccountRepo)
{
    public async Task<Result<MailboxAccountContext>> TryResolveAccountAsync(Guid userId, string? mailboxRef = null, CancellationToken cancellationToken = default)
    {
        var result = new Result<MailboxAccountContext>();
        cancellationToken.ThrowIfCancellationRequested();

        string? alias = null;
        if (!string.IsNullOrWhiteSpace(mailboxRef))
        {
            var trimmed = mailboxRef.Trim();
            if (EntityRefs.TryParse(trimmed, out var kind, out var parsedAlias))
            {
                if (kind != EntityRefs.Kind.Mailbox)
                {
                    result.Failure(
                        ErrorCode.BadRequest,
                        $"Expected a mailbox reference (mailbox:alias), got \"{trimmed}\".");
                    return result;
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
            var message = alias is not null
                ? MailboxMessages.AccountNotFound(alias)
                : MailboxMessages.NotConfigured;

            result.Failure(ErrorCode.NotFound, message);
            return result;
        }

        if (account.EmailProvider is null)
        {
            result.Failure(ErrorCode.NotFound, MailboxMessages.NotConfigured);
            return result;
        }

        var settings = EmailAccountMapping.ToStoredSettings(account, account.EmailProvider);
        var runtime = EmailSettingsMapping.ToMailRuntime(settings);
        if (runtime is null)
        {
            var message = alias is not null
                ? MailboxMessages.IncompleteAccount(account.EmailAddress)
                : MailboxMessages.NotConfigured;

            result.Failure(ErrorCode.NotFound, message);
            return result;
        }

        result.Success(new MailboxAccountContext
        {
            EmailAccountId = account.Id,
            Alias = account.Alias,
            EmailAddress = account.EmailAddress,
            ProviderName = account.EmailProvider.Name,
            IsDefault = account.IsDefault,
            Runtime = runtime
        });

        return result;
    }
}
