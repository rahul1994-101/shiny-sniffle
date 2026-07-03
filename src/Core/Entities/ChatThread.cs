using System.ComponentModel.DataAnnotations;

namespace Core.Entities;

public class ChatThread : BaseAuditableEntity
{
    [Required(ErrorMessage = "UserId is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    public ChatAgent ChatAgent { get; set; }

    /// <summary>Rolling summary of messages outside the short-term window.</summary>
    public string? MemorySummary { get; set; }

    /// <summary>Last message id included in <see cref="MemorySummary"/>.</summary>
    public Guid? MemorySummaryThroughMessageId { get; set; }
}
