namespace WebApp.AI.Contracts;

public sealed class ChatTurnRequest
{
    public Guid UserId { get; init; }

    public Guid ChatThreadId { get; init; }

    public string UserMessage { get; init; } = string.Empty;
}
