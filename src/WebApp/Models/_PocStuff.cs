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
        return $"Thanks — I received: “{preview}”. (Local mock reply — replace when the agent API is wired back in.)";
    }
}

public sealed class SettingsVM
{
    public string ModelName { get; set; } = "demo-model";
    public string Theme { get; set; } = "dark";
}
