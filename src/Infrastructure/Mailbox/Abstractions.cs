namespace Infrastructure.Mailbox;

public interface IMailboxService
{
    #region # Connection

    Task<TestConnectionResult> TestConnectionAsync(EmailSettings config, CancellationToken cancellationToken = default);

    #endregion

    #region # Queries

    Task<ListMessagesResult> ListMessagesAsync(EmailSettings config, ListMessagesFilters filters, CancellationToken cancellationToken = default);

    Task<GetMessagesResult> GetMessagesAsync(EmailSettings config, GetMessagesFilters filters, CancellationToken cancellationToken = default);

    Task<ListFoldersResult> ListFoldersAsync(EmailSettings config, CancellationToken cancellationToken = default);

    #endregion

    #region # Commands

    Task<SendMailResult> SendAsync(EmailSettings config, OutboundMail mail, CancellationToken cancellationToken = default);

    Task<MailboxCommandResult> DeleteMessagesAsync(EmailSettings config, IReadOnlyList<MessageKey> messages, CancellationToken cancellationToken = default);

    Task<MailboxCommandResult> MoveMessagesAsync(EmailSettings config, IReadOnlyList<MessageKey> messages, string destinationFolder, CancellationToken cancellationToken = default);

    Task<MailboxCommandResult> SetMessageFlagsAsync(EmailSettings config, IReadOnlyList<MessageKey> messages, MessageFlagAction flag, CancellationToken cancellationToken = default);

    #endregion
}
