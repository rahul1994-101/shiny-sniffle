using WebApp.AI.Agents;
using WebApp.Models;

namespace WebApp.AI.Orchestration;

public sealed class ChatAgentRouter(AssistantAgent assistantAgent, EmailAgent emailAgent)
{
    public Task<ChatTurnResult> RouteAsync(
        ChatAgent chatAgent,
        ChatTurnRequest request,
        MemoryContext memory,
        CancellationToken cancellationToken = default) =>
        chatAgent switch
        {
            ChatAgent.Email => emailAgent.RunAsync(request, memory, cancellationToken),
            _ => assistantAgent.RunAsync(request, memory, cancellationToken)
        };
}
