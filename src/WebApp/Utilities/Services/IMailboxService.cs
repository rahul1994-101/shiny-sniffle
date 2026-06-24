using WebApp.Models;

namespace WebApp.Utilities.Services;

public interface IMailboxService
{
    Task<MailboxStatusResult> GetStatusAsync(EmailSettings config, CancellationToken cancellationToken = default);

    Task<MailboxTestResult> TestConnectionAsync(EmailSettings config, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InboxMessageSummary>> ListInboxAsync(EmailSettings config, InboxQuery query, CancellationToken cancellationToken = default);

    Task<SendMailResult> SendAsync(EmailSettings config, OutboundMail mail, CancellationToken cancellationToken = default);
}
