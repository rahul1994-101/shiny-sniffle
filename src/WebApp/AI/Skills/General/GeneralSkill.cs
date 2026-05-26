using Microsoft.Extensions.AI;

using WebApp.AI.Configuration;
using WebApp.AI.Contracts;
using WebApp.AI.Infrastructure;
using WebApp.AI.Tools;

namespace WebApp.AI.Skills.General;

public sealed class GeneralSkill(AgentFactory agentFactory, WorkspaceTools workspaceTools)
{
    public async Task<ChatTurnResult> RunAsync(
        ChatTurnRequest request,
        MemoryContext memory,
        CancellationToken cancellationToken = default)
    {
        var profile = agentFactory.GetProfile(AgentProfileKeys.ChatGeneral);
        var tools = workspaceTools.CreateTools(request.UserId, request.ChatThreadId);
        var agent = agentFactory.CreateAgent(AgentProfileKeys.ChatGeneral, tools);

        var messages = memory.ToChatMessages();
        messages.Add(new ChatMessage(ChatRole.User, request.UserMessage));

        var response = await agent.RunAsync(messages, cancellationToken: cancellationToken);

        return new ChatTurnResult
        {
            AssistantContent = ExtractAssistantText(response),
            Intent = IntentKeys.GeneralChat,
            Handler = nameof(GeneralSkill),
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
