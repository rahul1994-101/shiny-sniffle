using Infrastructure.Mailbox;

namespace Application.Features.Workspace.EmailAccounts;

/// <summary>
/// Workspace email-account setup — merge stored/draft settings and probe the mail port (Path B).
/// Runtime agent operations use <see cref="Shared.WorkspaceMailboxService"/> with a resolved <see cref="MailboxAccountContext"/>.
/// </summary>
public sealed class EmailAccountMailboxService(EmailAccountRepository emailAccountRepo, IMailboxService mailboxService)
{
    public async Task<TestConnectionResult> TestConnectionWithDraftAsync(Guid userId, EmailSettingsDto? draft = null, CancellationToken cancellationToken = default)
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
}
