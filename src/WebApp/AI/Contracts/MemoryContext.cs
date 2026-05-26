using Microsoft.Extensions.AI;

namespace WebApp.AI.Contracts;

public sealed class MemoryContext
{
    public Guid ChatThreadId { get; init; }

    public IReadOnlyList<ChatMessage> Messages { get; init; } = [];

    public List<ChatMessage> ToChatMessages() => Messages.ToList();
}
