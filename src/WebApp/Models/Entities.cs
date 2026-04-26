namespace WebApp.Models;

public sealed class ChatThread
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = "New chat";
    public List<ChatMessage> Messages { get; } = [];
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Role { get; init; }
    public required string Content { get; init; }
}

public static class ChatMocks
{
    public static string AssistantReply(string userMessage)
    {
        var preview = userMessage.Length > 90 ? userMessage[..90].Trim() + "…" : userMessage.Trim();
        return $"Thanks — I received: “{preview}”. (Mock reply — edit ChatMocks.AssistantReply in Models/Entities.cs.)";
    }
}


public class User
{
    public bool IsDeleted { get; set; }
    public string Id { get; internal set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Mobile { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; }
}
