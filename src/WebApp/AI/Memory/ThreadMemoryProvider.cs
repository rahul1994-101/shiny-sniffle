using Microsoft.Extensions.AI;

using WebApp.Data;
using WebApp.Models;

namespace WebApp.AI.Memory;

public sealed class ThreadMemoryProvider(Persistence persistence)
{
    private const int DefaultMessageLimit = 12;

    public async Task<MemoryContext> LoadAsync(Guid chatThreadId, CancellationToken cancellationToken = default)
    {
        var messages = await persistence.GetChatMessagesByChatThreadIdAsync(
            new GetChatMessagesByChatThreadIdRequest { ChatThreadId = chatThreadId });

        var chatMessages = (messages ?? [])
            .OrderBy(m => m.CreatedAt)
            .TakeLast(DefaultMessageLimit)
            .Select(m => new Microsoft.Extensions.AI.ChatMessage(ToChatRole(m.Role), m.Content))
            .ToList();

        return new MemoryContext
        {
            ChatThreadId = chatThreadId,
            Messages = chatMessages
        };
    }

    private static ChatRole ToChatRole(string role) =>
        role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            ? ChatRole.Assistant
            : ChatRole.User;
}
