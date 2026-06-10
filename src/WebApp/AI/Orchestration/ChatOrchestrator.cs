using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using WebApp.AI.Agents;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.AI.Orchestration;

public sealed class ChatOrchestrator(
    IOptions<FoundryOptions> foundryOptions,
    Persistence persistence,
    AssistantAgent assistantAgent,
    EmailAgent emailAgent)
{
    private const int DefaultMessageLimit = 12;

    public async Task<ChatTurnResult> ProcessTurnAsync(
        ChatTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!foundryOptions.Value.IsConfigured)
        {
            return new ChatTurnResult
            {
                AssistantContent =
                    "AI is not configured yet. Set Foundry:Enabled, Foundry:Endpoint, and Foundry:ApiKey " +
                    "(user secrets locally or environment variables on Plesk)."
            };
        }

        var history = await LoadThreadHistoryAsync(request.ChatThreadId, cancellationToken);

        return request.ChatAgent switch
        {
            ChatAgent.Email => await emailAgent.RunAsync(request, history, cancellationToken),
            _ => await assistantAgent.RunAsync(request, history, cancellationToken)
        };
    }


    #region # Private Helpers

    private async Task<IReadOnlyList<Microsoft.Extensions.AI.ChatMessage>> LoadThreadHistoryAsync(
        Guid chatThreadId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var messages = await persistence.GetRecentChatMessagesByChatThreadIdAsync(
            new GetRecentChatMessagesByChatThreadIdRequest
            {
                ChatThreadId = chatThreadId,
                Limit = DefaultMessageLimit
            });

        return (messages ?? [])
            .Select(m => new Microsoft.Extensions.AI.ChatMessage(ToChatRole(m.Role), m.Content))
            .ToList();
    }

    private static ChatRole ToChatRole(string role) =>
        role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            ? ChatRole.Assistant
            : ChatRole.User;

    #endregion
}
