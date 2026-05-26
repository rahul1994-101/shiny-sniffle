using WebApp.AI.Agents.Intent;
using WebApp.AI.Contracts;
using WebApp.AI.Infrastructure;
using WebApp.AI.Memory;

namespace WebApp.AI.Orchestration;

public sealed class ChatOrchestrator(
    FoundryClientFactory foundryClientFactory,
    ThreadMemoryProvider threadMemoryProvider,
    IntentAgent intentAgent,
    IntentRouter intentRouter)
{
    public async Task<ChatTurnResult> ProcessTurnAsync(
        ChatTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!foundryClientFactory.IsConfigured)
        {
            return new ChatTurnResult
            {
                AssistantContent =
                    "AI is not configured yet. Set Foundry:Enabled and Foundry:ProjectEndpoint. " +
                    "For Plesk or local dev without az login, also set Foundry:ApiKey (user secrets or environment variable).",
                Intent = IntentKeys.GeneralChat,
                Handler = nameof(ChatOrchestrator)
            };
        }

        var memory = await threadMemoryProvider.LoadAsync(request.ChatThreadId, cancellationToken);
        var intent = await intentAgent.ClassifyAsync(request, memory, cancellationToken);
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
