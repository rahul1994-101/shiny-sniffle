namespace Infrastructure.Mailbox;

public interface IMailboxService
{
    #region # Connection

    Task<TestConnectionResult> TestConnectionAsync(EmailSettings config, CancellationToken cancellationToken = default);

    #endregion

    #region # Queries

    Task<ListMessagesResult> ListMessagesAsync(EmailSettings config, ListMessagesFilters filters, CancellationToken cancellationToken = default);

    Task<GetMessagesResult> GetMessagesAsync(EmailSettings config, MessageBatchFilters filters, CancellationToken cancellationToken = default);

    Task<ListFoldersResult> ListFoldersAsync(EmailSettings config, CancellationToken cancellationToken = default);

    #endregion

    #region # Commands

    Task<SendMailResult> SendAsync(EmailSettings config, OutboundMail mail, CancellationToken cancellationToken = default);

    Task<CommandResult> DeleteMessagesAsync(EmailSettings config, MessageBatchFilters filters, CancellationToken cancellationToken = default);

    Task<CommandResult> MoveMessagesAsync(EmailSettings config, MoveMessagesFilters filters, CancellationToken cancellationToken = default);

    Task<CommandResult> SetMessageFlagsAsync(EmailSettings config, SetMessageFlagsFilters filters, CancellationToken cancellationToken = default);

    #endregion
}
