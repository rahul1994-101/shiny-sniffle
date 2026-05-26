using Microsoft.Extensions.AI;

using WebApp.AI.Agents;
using WebApp.AI.Contracts;
using WebApp.Utilities.Helpers;

namespace WebApp.AI.Agents.Chat;

public sealed class GeneralChatAgent(FoundryAgentFactory agentFactory)
{
    public async Task<ChatTurnResult> RunAsync(
        ChatTurnRequest request,
        MemoryContext memory,
        CancellationToken cancellationToken = default)
    {
        var profile = agentFactory.GetProfile(AgentProfileKeys.ChatGeneral);
        var agent = agentFactory.CreateAgent(AgentProfileKeys.ChatGeneral);

        var messages = memory.ToChatMessages();
        messages.Add(new ChatMessage(ChatRole.User, request.UserMessage));

        var response = await agent.RunAsync(messages, cancellationToken: cancellationToken);

        return new ChatTurnResult
        {
            AssistantContent = ExtractAssistantText(response),
            Intent = IntentKeys.GeneralChat,
            Handler = nameof(GeneralChatAgent),
            ModelDeployment = profile.ModelDeployment
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
