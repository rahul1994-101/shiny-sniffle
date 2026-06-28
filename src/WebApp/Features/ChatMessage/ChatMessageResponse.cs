namespace WebApp.Features.ChatMessage;

public sealed class ChatMessageResponse
{
    public Guid Id { get; init; }
    public Guid ChatThreadId { get; init; }
    public string Role { get; init; } = "user";
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    internal static ChatMessageResponse FromEntity(Core.Entities.ChatMessage message) => new()
    {
        Id = message.Id,
        ChatThreadId = message.ChatThreadId,
        Role = message.Role,
        Content = message.Content,
        CreatedAt = message.CreatedAt
    };

    internal static List<ChatMessageResponse> FromEntities(IEnumerable<Core.Entities.ChatMessage> messages) =>
        messages.Select(FromEntity).ToList();
}
