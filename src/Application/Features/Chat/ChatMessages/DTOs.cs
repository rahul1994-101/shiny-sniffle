namespace Application.Features.Chat.ChatMessages;

public class ChatMessageDto
{
    public Guid Id { get; init; }
    public Guid ThreadId { get; init; }
    public string Role { get; init; } = "user";
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public static ChatMessageDto FromEntity(ChatMessage message) => new()
    {
        Id = message.Id,
        ThreadId = message.ChatThreadId,
        Role = message.Role,
        Content = message.Content,
        CreatedAt = message.CreatedAt
    };

    public static List<ChatMessageDto> FromEntities(IEnumerable<ChatMessage> messages) =>
        messages.Select(FromEntity).ToList();

    public T AsResponse<T>() where T : ChatMessageDto, new() => new()
    {
        Id = Id,
        ThreadId = ThreadId,
        Role = Role,
        Content = Content,
        CreatedAt = CreatedAt
    };
}
