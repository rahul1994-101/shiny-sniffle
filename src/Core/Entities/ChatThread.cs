namespace Core.Entities;

public class ChatThread : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public ChatAgent ChatAgent { get; set; }

    /// <summary>Rolling summary of messages outside the short-term window.</summary>
    public string? MemorySummary { get; set; }

    /// <summary>Last message id included in <see cref="MemorySummary"/>.</summary>
    public Guid? MemorySummaryThroughMessageId { get; set; }
}
