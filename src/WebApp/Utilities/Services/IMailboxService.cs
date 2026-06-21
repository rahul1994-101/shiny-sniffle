using WebApp.Models;

namespace WebApp.Utilities.Services;

public interface IMailboxService
{
    Task<MailboxStatusResult> GetStatusAsync(MailboxConnectionOptions config, CancellationToken cancellationToken = default);

    Task<MailboxTestResult> TestConnectionAsync(MailboxConnectionOptions config, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InboxMessageSummary>> ListInboxAsync(
        MailboxConnectionOptions config,
        InboxQuery query,
        CancellationToken cancellationToken = default);

    Task<SendMailResult> SendAsync(
        MailboxConnectionOptions config,
        OutboundMail mail,
        CancellationToken cancellationToken = default);
}
