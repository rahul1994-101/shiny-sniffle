using Microsoft.Extensions.AI;

using WebApp.Data;
using WebApp.Models;

namespace WebApp.AI.Memory;

public sealed class ThreadMemoryProvider(Persistence persistence)
{
    private const int DefaultMessageLimit = 12;

    public async Task<IReadOnlyList<Microsoft.Extensions.AI.ChatMessage>> LoadAsync(
        Guid chatThreadId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var messages = await persistence.GetChatMessagesByChatThreadIdAsync(
            new GetChatMessagesByChatThreadIdRequest { ChatThreadId = chatThreadId });

        return (messages ?? [])
            .OrderBy(m => m.CreatedAt)
            .TakeLast(DefaultMessageLimit)
            .Select(m => new Microsoft.Extensions.AI.ChatMessage(ToChatRole(m.Role), m.Content))
            .ToList();
    }

    private static ChatRole ToChatRole(string role) =>
        role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            ? ChatRole.Assistant
            : ChatRole.User;
}
