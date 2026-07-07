namespace Core.Entities;

public class ChatMessage : BaseAuditableEntity
{
    public Guid ChatThreadId { get; set; }

    public string Role { get; set; } = "user";

    public string Content { get; set; } = string.Empty;
}
