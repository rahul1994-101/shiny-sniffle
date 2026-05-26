using Microsoft.Extensions.AI;

using WebApp.AI.Agents;
using WebApp.Models;
using WebApp.Utilities.Helpers;

namespace WebApp.AI.Agents.Intent;

public sealed class IntentClassificationAgent(FoundryAgentFactory agentFactory)
{
    public async Task<IntentResult> ClassifyAsync(
        ChatTurnRequest request,
        MemoryContext memory,
        CancellationToken cancellationToken = default)
    {
        var agent = agentFactory.CreateAgent(AgentProfileKeys.IntentRouter);
        var messages = memory.ToChatMessages();
        messages.Add(new Microsoft.Extensions.AI.ChatMessage(
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

    private static string NormalizeIntent(string intent)
    {
        var normalized = intent.Trim().ToLowerInvariant();
        return normalized switch
        {
            IntentKeys.GeneralChat => IntentKeys.GeneralChat,
            _ => normalized
        };
    }
}
