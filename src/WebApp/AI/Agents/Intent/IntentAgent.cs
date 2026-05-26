using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using WebApp.AI.Configuration;
using WebApp.AI.Contracts;
using WebApp.AI.Infrastructure;

namespace WebApp.AI.Agents.Intent;

public sealed class IntentAgent(AgentFactory agentFactory)
{
    public async Task<IntentResult> ClassifyAsync(
        ChatTurnRequest request,
        MemoryContext memory,
        CancellationToken cancellationToken = default)
    {
        var agent = agentFactory.CreateAgent(AgentProfileKeys.IntentRouter);
        var messages = memory.ToChatMessages();
        messages.Add(new ChatMessage(
            ChatRole.User,
            $"""
             Classify this user message.

             User message:
             {request.UserMessage}
             """));

        var response = await agent.RunAsync<IntentResult>(
            messages,
            cancellationToken: cancellationToken);

        var intent = response.Result;
        if (intent is null || string.IsNullOrWhiteSpace(intent.Intent))
        {
            return new IntentResult
            {
                Intent = IntentKeys.GeneralChat,
                Confidence = 0,
                Reason = "Intent agent returned no structured result."
            };
        }

        intent.Intent = NormalizeIntent(intent.Intent);
        return intent;
    }

    private static string NormalizeIntent(string intent) =>
        intent.Trim().ToLowerInvariant() switch
        {
            IntentKeys.WorkspaceInfo => IntentKeys.WorkspaceInfo,
            IntentKeys.GeneralChat => IntentKeys.GeneralChat,
            _ => IntentKeys.GeneralChat
        };
}
