using Microsoft.Extensions.AI;

using WebApp.AI.Agents;
using WebApp.Models;
using WebApp.Utilities.Helpers;

namespace WebApp.AI.Agents.Chat;

public sealed class GeneralChatAgent(FoundryAgentFactory agentFactory)
{
    public async Task<ChatTurnResult> RunAsync(
        ChatTurnRequest request,
        MemoryContext memory,
        CancellationToken cancellationToken = default)
    {
        var agent = agentFactory.CreateAgent(AgentProfileKeys.ChatGeneral);

        var messages = memory.ToChatMessages();
        messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, request.UserMessage));

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
