using System.Net.Mail;

namespace Infrastructure.Mailbox;

public sealed class MailKitMailboxService : IMailboxService
{
    private static readonly MailboxCommandResult NoMessagesSpecified = new()
    {
        Success = false,
        Message = "No messages were specified."
    };

    #region # Connection

    public async Task<TestConnectionResult> TestConnectionAsync(EmailSettings config, CancellationToken cancellationToken = default)
    {
        var probe = await MailboxConnectionHelpers.ProbeConnectionsAsync(config, cancellationToken);

        return new TestConnectionResult
        {
            ImapOk = probe.ImapOk,
            SmtpOk = probe.SmtpOk,
            Message = probe.ImapOk && probe.SmtpOk
                ? "IMAP and SMTP are reachable."
                : MailboxConnectionHelpers.FormatConnectionProbeMessage(probe)
        };
    }

    #endregion

    #region # Queries

    public Task<ListMessagesResult> ListMessagesAsync(EmailSettings config, ListMessagesFilters filters, CancellationToken cancellationToken = default) =>
        MailboxConnectionHelpers.ExecuteImapAsync(config, (imap, ct) => MailboxSummaryHelpers.ListAsync(imap, filters, ct), cancellationToken);

    public async Task<GetMessagesResult> GetMessagesAsync(EmailSettings config, GetMessagesFilters filters, CancellationToken cancellationToken = default)
    {
        var messages = filters.Messages;
        if (messages.Count == 0)
        {
            return new GetMessagesResult();
        }

        if (messages.Count > MailboxReadLimits.MaxBatchGetCount)
        {
            throw new ArgumentException(
                $"At most {MailboxReadLimits.MaxBatchGetCount} messages can be read per call.",
                nameof(filters));
        }

        var details = await MailboxConnectionHelpers.ExecuteImapAsync(config, (imap, ct) => MailboxMessageHelpers.GetManyAsync(imap, messages, ct), cancellationToken);
        return new GetMessagesResult { Messages = details };
    }

    public async Task<ListFoldersResult> ListFoldersAsync(EmailSettings config, CancellationToken cancellationToken = default)
    {
        var folders = await MailboxConnectionHelpers.ExecuteImapAsync(config, MailboxFolderResolverHelpers.ListAllAsync, cancellationToken);
        return new ListFoldersResult { Folders = folders };
    }

    #endregion

    #region # Commands

    public async Task<SendMailResult> SendAsync(EmailSettings config, OutboundMail mail, CancellationToken cancellationToken = default)
    {
        if (!MailAddress.TryCreate(mail.To, out _))
        {
            return new SendMailResult
            {
                Success = false,
                Message = "Recipient email address is invalid."
            };
        }

        return await MailboxConnectionHelpers.ExecuteSmtpAsync(config, (smtp, ct) => MailboxSendHelpers.SendAsync(smtp, config, mail, ct), cancellationToken);
    }

    public async Task<MailboxCommandResult> DeleteMessagesAsync(EmailSettings config, IReadOnlyList<MessageKey> messages, CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return NoMessagesSpecified;
        }

        return await MailboxConnectionHelpers.ExecuteImapAsync(config, (imap, ct) => MailboxCommandsHelpers.DeleteAsync(imap, messages, ct), cancellationToken);
    }

    public async Task<MailboxCommandResult> MoveMessagesAsync(EmailSettings config, IReadOnlyList<MessageKey> messages, string destinationFolder, CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return NoMessagesSpecified;
        }

        if (string.IsNullOrWhiteSpace(destinationFolder))
        {
            return new MailboxCommandResult
            {
                Success = false,
                Message = "Destination folder is required."
            };
        }

        var destination = destinationFolder.Trim();
        return await MailboxConnectionHelpers.ExecuteImapAsync(config, (imap, ct) => MailboxCommandsHelpers.MoveAsync(imap, messages, destination, ct), cancellationToken);
    }

    public async Task<MailboxCommandResult> SetMessageFlagsAsync(EmailSettings config, IReadOnlyList<MessageKey> messages, MessageFlagAction flag, CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return NoMessagesSpecified;
        }

        return await MailboxConnectionHelpers.ExecuteImapAsync(config, (imap, ct) => MailboxCommandsHelpers.SetFlagsAsync(imap, messages, flag, ct), cancellationToken);
    }

    #endregion
}
