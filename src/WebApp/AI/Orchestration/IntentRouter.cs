using WebApp.AI.Agents.Chat;
using WebApp.Models;

namespace WebApp.AI.Orchestration;

public sealed class IntentRouter(GeneralChatAgent generalChatAgent)
{
    public Task<ChatTurnResult> RouteAsync(
        string intent,
        ChatTurnRequest request,
        MemoryContext memory,
        CancellationToken cancellationToken = default) =>
        intent switch
        {
            IntentKeys.GeneralChat => generalChatAgent.RunAsync(request, memory, cancellationToken),
            // Add new intent cases here and register handlers in AiServiceRegistration.
            _ => generalChatAgent.RunAsync(request, memory, cancellationToken)
        };
}
