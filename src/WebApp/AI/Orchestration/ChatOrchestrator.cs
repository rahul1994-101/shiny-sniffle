using Microsoft.Extensions.Options;

using WebApp.AI.Agents.Intent;
using WebApp.AI.Configuration;
using WebApp.AI.Contracts;
using WebApp.AI.Memory;

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
                    "(user secrets locally or environment variables on Plesk).",
                Intent = IntentKeys.GeneralChat,
                Handler = nameof(ChatOrchestrator)
            };
        }

        var memory = await threadMemoryProvider.LoadAsync(request.ChatThreadId, cancellationToken);
        var intent = await intentClassificationAgent.ClassifyAsync(request, memory, cancellationToken);
        var result = await intentRouter.RouteAsync(intent.Intent, request, memory, cancellationToken);

        return new ChatTurnResult
        {
            AssistantContent = result.AssistantContent,
            Handler = result.Handler,
            ModelDeployment = result.ModelDeployment,
            Intent = intent.Intent
        };
    }
}
