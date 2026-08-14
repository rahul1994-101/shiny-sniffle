namespace Application.Features.Chat.ChatThreads;

public sealed record ThreadMemoryState(string? Summary, Guid? SummaryThroughMessageId);

public class ChatThreadDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public ChatAgent ChatAgent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static ChatThreadDto FromEntity(ChatThread thread) => new()
    {
        Id = thread.Id,
        Title = thread.Title,
        UserId = thread.UserId,
        ChatAgent = ChatAgentHelpers.ToModel(thread.ChatAgent),
        CreatedAt = thread.CreatedAt,
        UpdatedAt = thread.UpdatedAt
    };

    public static List<ChatThreadDto> FromEntities(IEnumerable<ChatThread> threads) =>
        threads.Select(FromEntity).ToList();

    public T AsResponse<T>() where T : ChatThreadDto, new() => new()
    {
        Id = Id,
        Title = Title,
        UserId = UserId,
        ChatAgent = ChatAgent,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt
    };
}
