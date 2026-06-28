namespace Core.DTOs;

public class ChatMessageDto
{
    public Guid Id { get; set; }

    public Guid ChatThreadId { get; set; }

    public string Role { get; set; } = "user";

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
