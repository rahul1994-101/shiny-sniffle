using Microsoft.Extensions.AI;

using WebApp.Data;
using WebApp.Models;

namespace WebApp.AI.Tools;

public sealed class WorkspaceTools(Persistence persistence)
{
    public IList<AITool> CreateTools(Guid userId, Guid chatThreadId) =>
    [
        AIFunctionFactory.Create(
            () => GetConversationCountAsync(userId),
            name: "get_conversation_count",
            description: "Returns how many chat threads the signed-in user has."),
        AIFunctionFactory.Create(
            () => GetMessageCountInThreadAsync(chatThreadId),
            name: "get_message_count_in_thread",
            description: "Returns how many messages exist in the current chat thread."),
        AIFunctionFactory.Create(
            () => GetWorkspaceSnapshotAsync(userId, chatThreadId),
            name: "get_workspace_snapshot",
            description: "Returns a short workspace summary for the current user and thread.")
    ];

    public async Task<int> GetConversationCountAsync(Guid userId)
    {
        var threads = await persistence.GetChatThreadsByUserIdAsync(
            new GetChatThreadsByUserIdRequest { UserId = userId });

        return threads?.Count ?? 0;
    }

    public async Task<int> GetMessageCountInThreadAsync(Guid chatThreadId)
    {
        var messages = await persistence.GetChatMessagesByChatThreadIdAsync(
            new GetChatMessagesByChatThreadIdRequest { ChatThreadId = chatThreadId });

        return messages?.Count ?? 0;
    }

    public async Task<string> GetWorkspaceSnapshotAsync(Guid userId, Guid chatThreadId)
    {
        var threadCount = await GetConversationCountAsync(userId);
        var messageCount = await GetMessageCountInThreadAsync(chatThreadId);

        return
            $"threads={threadCount}; messages_in_current_thread={messageCount}; user_id={userId}; thread_id={chatThreadId}";
    }
}
