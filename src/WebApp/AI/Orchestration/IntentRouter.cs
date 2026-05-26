using WebApp.AI.Contracts;
using WebApp.AI.Skills.General;
using WebApp.AI.Workflows.Workspace;

namespace WebApp.AI.Orchestration;

public sealed class IntentRouter(
    GeneralSkill generalSkill,
    WorkspaceInfoWorkflow workspaceInfoWorkflow)
{
    public Task<ChatTurnResult> RouteAsync(
        string intent,
        ChatTurnRequest request,
        MemoryContext memory,
        CancellationToken cancellationToken = default) =>
        intent switch
        {
            IntentKeys.WorkspaceInfo => workspaceInfoWorkflow.RunAsync(request, memory, cancellationToken),
            _ => generalSkill.RunAsync(request, memory, cancellationToken)
        };
}
