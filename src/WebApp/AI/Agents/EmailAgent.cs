using Microsoft.Extensions.AI;

using WebApp.AI.Foundry;
using WebApp.AI.Tools;
using WebApp.Models;

namespace WebApp.AI.Agents;

public sealed class EmailAgent(FoundryAgentFactory _agentFactory, EmailTools _emailTools)
{
    public async Task<RunChatAgentResponse> RunAsync(
        RunChatAgentRequest request,
        IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        #region # Execute

        var tools = _emailTools.CreateTools(request.UserId, request.ChatThreadId);
        var agent = _agentFactory.CreateEmailAgent(tools);
        var messages = history.ToList();
        var response = await agent.RunAsync(messages, cancellationToken: cancellationToken);

        #endregion

        #region # Handle Result

        return new RunChatAgentResponse
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
