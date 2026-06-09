using Microsoft.Extensions.Options;

using WebApp.AI.Agents;
using WebApp.AI.Memory;
using WebApp.Models;

namespace WebApp.AI.Orchestration;

public sealed class ChatOrchestrator(
    IOptions<FoundryOptions> foundryOptions,
    ThreadMemoryProvider threadMemoryProvider,
    AssistantAgent assistantAgent,
    EmailAgent emailAgent)
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

        var history = await threadMemoryProvider.LoadAsync(request.ChatThreadId, cancellationToken);

        return request.ChatAgent switch
        {
            ChatAgent.Email => await emailAgent.RunAsync(request, history, cancellationToken),
            _ => await assistantAgent.RunAsync(request, history, cancellationToken)
        };
    }
}
