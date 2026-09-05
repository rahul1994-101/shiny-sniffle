namespace Infrastructure.Persistence.Chat;

/// <summary>Last mailbox list snapshot for one Email chat thread + mailbox alias (working memory, not conversation summary).</summary>
public class EmailThreadMemory
{
    public Guid ChatThreadId { get; set; }

    public string MailboxAlias { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string ListSnapshotJson { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
