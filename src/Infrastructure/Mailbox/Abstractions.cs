namespace Infrastructure.Mailbox;

public interface IMailboxService
{
    Task<MailboxStatusResult> GetStatusAsync(EmailSettings config, CancellationToken cancellationToken = default);

    Task<MailboxTestResult> TestConnectionAsync(EmailSettings config, CancellationToken cancellationToken = default);

    Task<InboxListResult> ListMessagesAsync(EmailSettings config, InboxQuery query, CancellationToken cancellationToken = default);

    Task<InboxMessageDetail?> GetMessageAsync(EmailSettings config, uint uid, string? folder = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InboxMessageDetail>> GetMessagesAsync(EmailSettings config, IReadOnlyList<MessageRef> messages, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MailboxFolderInfo>> ListFoldersAsync(EmailSettings config, CancellationToken cancellationToken = default);

    Task<SendMailResult> SendAsync(EmailSettings config, OutboundMail mail, CancellationToken cancellationToken = default);

    Task<MailboxCommandResult> DeleteMessagesAsync(EmailSettings config, IReadOnlyList<MessageRef> messages, CancellationToken cancellationToken = default);

    Task<MailboxCommandResult> MoveMessagesAsync(EmailSettings config, IReadOnlyList<MessageRef> messages, string destinationFolder, CancellationToken cancellationToken = default);

    Task<MailboxCommandResult> SetMessageFlagsAsync(EmailSettings config, IReadOnlyList<MessageRef> messages, MessageFlagAction flag, CancellationToken cancellationToken = default);
}
