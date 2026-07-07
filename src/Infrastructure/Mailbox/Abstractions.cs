using Core.Entities;

namespace Infrastructure.Mailbox;

public interface IMailboxService
{
    Task<MailboxStatusResult> GetStatusAsync(EmailSettings config, CancellationToken cancellationToken = default);

    Task<MailboxTestResult> TestConnectionAsync(EmailSettings config, CancellationToken cancellationToken = default);

    Task<InboxListResult> ListInboxAsync(EmailSettings config, InboxQuery query, CancellationToken cancellationToken = default);

    Task<InboxMessageDetail?> GetInboxMessageAsync(EmailSettings config, uint uid, string? folder = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MailboxFolderInfo>> ListFoldersAsync(EmailSettings config, CancellationToken cancellationToken = default);

    Task<SendMailResult> SendAsync(EmailSettings config, OutboundMail mail, CancellationToken cancellationToken = default);
}
