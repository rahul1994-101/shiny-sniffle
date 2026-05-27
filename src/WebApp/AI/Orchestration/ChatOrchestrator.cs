using Microsoft.Extensions.Options;

using WebApp.AI.Agents.Intent;
using WebApp.AI.Memory;
using WebApp.Models;

namespace WebApp.AI.Orchestration;

public sealed class ChatOrchestrator(
    IOptions<FoundryOptions> foundryOptions,
    ThreadMemoryProvider threadMemoryProvider,
    IntentClassificationAgent intentClassificationAgent,
    IntentRouter intentRouter)
{
    public async Task<ChatTurnResult> ProcessTurnAsync(
        ChatTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!foundryOptions.Value.IsConfigured)
        {
            return new ChatTurnResult
            {
                AssistantContent =
                    "AI is not configured yet. Set Foundry:Enabled, Foundry:Endpoint, and Foundry:ApiKey " +
                    "(user secrets locally or environment variables on Plesk)."
            };
        }

        var memory = await threadMemoryProvider.LoadAsync(request.ChatThreadId, cancellationToken);
        var intent = await intentClassificationAgent.ClassifyAsync(request, memory, cancellationToken);
        return await intentRouter.RouteAsync(intent.Intent, request, memory, cancellationToken);
    }
}
