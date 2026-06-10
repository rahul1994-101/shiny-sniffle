using Microsoft.Extensions.AI;

using WebApp.AI.Foundry;
using WebApp.Models;

namespace WebApp.AI.Agents;

public sealed class AssistantAgent(FoundryAgentFactory _agentFactory)
{
    public async Task<ChatTurnResult> RunAsync(
        ChatTurnRequest request,
        IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        #region # Execute

        var agent = _agentFactory.CreateAssistantAgent();
        var messages = history.ToList();
        var response = await agent.RunAsync(messages, cancellationToken: cancellationToken);

        #endregion

        #region # Handle Result

        return new ChatTurnResult
        {
            AssistantContent = ExtractAssistantText(response)
        };

        #endregion
    }

    #region # Private Helpers

    private static string ExtractAssistantText(Microsoft.Agents.AI.AgentResponse response)
    {
        var text = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text;
        return string.IsNullOrWhiteSpace(text)
            ? "I could not generate a response."
            : text.Trim();
    }

    #endregion
}
