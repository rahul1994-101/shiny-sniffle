using Microsoft.Agents.AI;
using WebApp.Models;

namespace WebApp.AI.Agents;

public sealed class AssistantAgent(FoundryAgentFactory _agentFactory)
{
    public async Task<RunChatAgentResponse> RunAsync(
        RunChatAgentRequest request,
        IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        #region # Execute

        var agent = CreateAssistantAgent();
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

    private AIAgent CreateAssistantAgent()
    {
        var modelDeployment = "gpt-4o-mini-deploy";
        var name = "Assistant";
        var description = "General conversational assistant.";
        var instructions =
            "You are a helpful workspace assistant. Be concise and friendly. " +
            "You do not have access to email or mailbox tools. " +
            "If the user needs mail help, suggest switching to the Email agent in the chat composer.";

        return _agentFactory.CreateAgent(modelDeployment, name, description, instructions);
    }

    private static string ExtractAssistantText(Microsoft.Agents.AI.AgentResponse response)
    {
        var text = response.Messages.LastOrDefault(m => m.Role == Microsoft.Extensions.AI.ChatRole.Assistant)?.Text;
        return string.IsNullOrWhiteSpace(text)
            ? "I could not generate a response."
            : text.Trim();
    }

    #endregion
}
