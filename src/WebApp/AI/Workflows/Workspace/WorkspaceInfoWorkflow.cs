using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

using WebApp.AI.Configuration;
using WebApp.AI.Contracts;
using WebApp.AI.Infrastructure;
using WebApp.AI.Tools;

namespace WebApp.AI.Workflows.Workspace;

public sealed class WorkspaceInfoWorkflow(AgentFactory agentFactory, WorkspaceTools workspaceTools)
{
    public async Task<ChatTurnResult> RunAsync(
        ChatTurnRequest request,
        MemoryContext memory,
        CancellationToken cancellationToken = default)
    {
        var tools = workspaceTools.CreateTools(request.UserId, request.ChatThreadId);
        var dataAgent = agentFactory.CreateAgent(AgentProfileKeys.WorkspaceData, tools);
        var presenterAgent = agentFactory.CreateAgent(AgentProfileKeys.WorkspacePresenter);

        var workflow = AgentWorkflowBuilder.BuildSequential(
            "workspace-info",
            [dataAgent, presenterAgent]);

        var messages = memory.ToChatMessages();
        messages.Add(new ChatMessage(ChatRole.User, request.UserMessage));

        await using StreamingRun run = await InProcessExecution
            .RunStreamingAsync(workflow, messages, cancellationToken: cancellationToken);

        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        List<ChatMessage>? finalMessages = null;
        await foreach (var workflowEvent in run.WatchStreamAsync().WithCancellation(cancellationToken))
        {
            if (workflowEvent is WorkflowOutputEvent outputEvent)
            {
                finalMessages = outputEvent.As<List<ChatMessage>>();
                break;
            }
        }

        var profile = agentFactory.GetProfile(AgentProfileKeys.WorkspacePresenter);
        var assistantText = finalMessages?
            .LastOrDefault(m => m.Role == ChatRole.Assistant)?
            .Text;

        return new ChatTurnResult
        {
            AssistantContent = string.IsNullOrWhiteSpace(assistantText)
                ? "I could not summarize your workspace."
                : assistantText.Trim(),
            Intent = IntentKeys.WorkspaceInfo,
            Handler = nameof(WorkspaceInfoWorkflow),
            ModelDeployment = profile.ModelDeployment
        };
    }
}
