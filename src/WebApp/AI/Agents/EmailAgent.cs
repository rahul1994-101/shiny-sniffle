using Microsoft.Extensions.AI;

using WebApp.AI.Foundry;
using WebApp.AI.Tools.Email;
using WebApp.Models;

namespace WebApp.AI.Agents;

public sealed class EmailAgent(FoundryAgentFactory agentFactory, EmailTools emailTools)
{
    public async Task<ChatTurnResult> RunAsync(
        ChatTurnRequest request,
        IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        var tools = emailTools.CreateTools(request.UserId, request.ChatThreadId);
        var agent = agentFactory.CreateEmailAgent(tools);
        var messages = history.ToList();

        var response = await agent.RunAsync(messages, cancellationToken: cancellationToken);

        return new ChatTurnResult
        {
            AssistantContent = ExtractAssistantText(response)
        };
    }

    private static string ExtractAssistantText(Microsoft.Agents.AI.AgentResponse response)
    {
        var text = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text;
        return string.IsNullOrWhiteSpace(text)
            ? "I could not generate a response."
            : text.Trim();
    }
}
