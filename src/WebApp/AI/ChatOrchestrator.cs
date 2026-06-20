using Microsoft.Extensions.Options;

using WebApp.AI.Agents;
using WebApp.Data;
using WebApp.Models;

using AiChatRole = Microsoft.Extensions.AI.ChatRole;

namespace WebApp.AI;

public sealed class ChatOrchestrator(
    IOptions<FoundryOptions> _foundryOptions,
    Persistence _persistence,
    AssistantAgent _assistantAgent,
    EmailAgent _emailAgent)
{
    private const int DefaultMessageLimit = 12;

    public async Task<RunChatAgentResponse> RunChatAgentAsync(
        RunChatAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        #region # Validate

        if (!_foundryOptions.Value.IsConfigured)
        {
            return new RunChatAgentResponse
            {
                AssistantContent =
                    "AI is not configured yet. Set Foundry:Enabled, Foundry:Endpoint, and Foundry:ApiKey " +
                    "(user secrets locally or environment variables on Plesk)."
            };
        }

        #endregion

        #region # Execute

        var history = await LoadThreadHistoryAsync(request.ChatThreadId, cancellationToken);
        var response = request.ChatAgent switch
        {
            ChatAgent.Email => await _emailAgent.RunAsync(request, history, cancellationToken),
            _ => await _assistantAgent.RunAsync(request, history, cancellationToken)
        };

        #endregion

        #region # Handle Result

        return response;

        #endregion
    }

    #region # Private Helpers

    private async Task<IReadOnlyList<Microsoft.Extensions.AI.ChatMessage>> LoadThreadHistoryAsync(
        Guid chatThreadId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var messages = await _persistence.GetRecentChatMessagesByChatThreadIdAsync(
            chatThreadId,
            DefaultMessageLimit);

        return (messages ?? [])
            .Select(m => new Microsoft.Extensions.AI.ChatMessage(new AiChatRole(m.Role), m.Content))
            .ToList();
    }

    #endregion
}
