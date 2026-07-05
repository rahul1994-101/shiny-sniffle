using Microsoft.Extensions.Options;

using WebApp.AI.Agents;
using WebApp.AI.Memory;
using WebApp.Features.ChatMessages;

using AiChatRole = Microsoft.Extensions.AI.ChatRole;

namespace WebApp.AI;

public sealed class ChatOrchestrator(
    IOptions<FoundryOptions> foundryOptions,
    ChatMessageRepository chatMessageRepo,
    ThreadMemoryService threadMemory,
    AssistantAgent assistantAgent,
    EmailAgent emailAgent)
{
    public async Task<RunChatAgentResponse> RunChatAgentAsync(RunChatAgentRequest request, CancellationToken cancellationToken = default)
    {
        #region # Validate

        if (!foundryOptions.Value.IsConfigured)
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
        history = await threadMemory.EnrichHistoryAsync(request.ChatThreadId, history, cancellationToken);
        var response = request.ChatAgent switch
        {
            ChatAgent.Email => await emailAgent.RunAsync(request, history, cancellationToken),
            _ => await assistantAgent.RunAsync(request, history, cancellationToken)
        };

        #endregion

        #region # Handle Result

        return response;

        #endregion
    }

    #region # Private Helpers

    private async Task<IReadOnlyList<Microsoft.Extensions.AI.ChatMessage>> LoadThreadHistoryAsync(Guid chatThreadId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var messages = await chatMessageRepo.GetRecentByChatThreadIdAsync(
            chatThreadId,
            ChatMemoryLimits.ShortTermMessageLimit,
            cancellationToken);

        return (messages ?? [])
            .Select(m => new Microsoft.Extensions.AI.ChatMessage(new AiChatRole(m.Role), m.Content))
            .ToList();
    }

    #endregion
}
