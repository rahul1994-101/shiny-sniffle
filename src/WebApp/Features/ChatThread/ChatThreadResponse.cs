namespace WebApp.Features.ChatThread;

public sealed class ChatThreadResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public ChatAgent ChatAgent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    internal static ChatThreadResponse FromEntity(Core.Entities.ChatThread thread) => new()
    {
        Id = thread.Id,
        Title = thread.Title,
        UserId = thread.UserId,
        ChatAgent = thread.ChatAgent,
        CreatedAt = thread.CreatedAt,
        UpdatedAt = thread.UpdatedAt
    };

    internal static List<ChatThreadResponse> FromEntities(IEnumerable<Core.Entities.ChatThread> threads) =>
        threads.Select(FromEntity).ToList();
}
