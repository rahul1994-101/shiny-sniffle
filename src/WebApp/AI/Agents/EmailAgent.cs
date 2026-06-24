using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using WebApp.AI.Tools;
using WebApp.Models;

namespace WebApp.AI.Agents;

public sealed class EmailAgent(FoundryAgentFactory _agentFactory, EmailTools _emailTools)
{
    public async Task<RunChatAgentResponse> RunAsync(RunChatAgentRequest request, IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> history, CancellationToken cancellationToken = default)
    {
        #region # Execute

        var tools = _emailTools.CreateTools(request.UserId, request.ChatThreadId);
        var agent = CreateEmailAgent(tools);
        var messages = history.ToList();
        var response = await agent.RunAsync(messages, cancellationToken: cancellationToken);

        #endregion

        #region # Handle Result

        return new RunChatAgentResponse
        {
            AssistantContent = ExtractAssistantText(response)
        };

        #endregion
    }

    #region # Private Helpers

    private AIAgent CreateEmailAgent(IList<AITool> tools)
    {
        var modelDeployment = FoundryDeployments.Gpt4oMini;
        var name = "Email";
        var description = "Mailbox specialist that lists, summarizes, and sends mail through the connected account.";
        var instructions = $"""
            You are the Email specialist for this workspace, not the general receptionist.

            Your job:
            - Help users read, summarize, and send email from their connected mailbox using your tools.
            - Use tools for every mailbox operation; summarize tool results clearly for the user.
            - Do not guess or invent message contents, send outcomes, or mailbox status.

            Tool rules:
            - For read, list, or summarize requests, call list_inbox_messages with since (today, yesterday, last_week, or yyyy-MM-dd) and a sensible limit.
            - For send requests, confirm recipient, subject, and body with the user before calling send_email.
            - If the mailbox may not be set up or reachable, call get_mailbox_status and relay the result; direct the user to Settings → Email when needed.
            - Only report mail content and send results returned by tools. Do not invent agents, tools, or capabilities.

            Boundaries:
            - Stay on topic: connected mailbox and email tasks only.
            - Do not answer general knowledge, coding help, trivia, chit-chat, or other off-topic requests.
            - For off-topic requests, decline briefly: you only help with mailbox and email—redirect to an email task or the right agent switch.
            - If the request is not about email or the mailbox, tell the user:
              "This is handled by the Assistant agent. Please switch to Assistant using the agent selector in the chat composer."
            - Keep replies concise, warm, and professional.
            """;

        return _agentFactory.CreateAgent(modelDeployment, name, description, instructions, tools);
    }

    private static string ExtractAssistantText(Microsoft.Agents.AI.AgentResponse response)
    {
        var text = response.Messages.LastOrDefault(m => m.Role == Microsoft.Extensions.AI.ChatRole.Assistant)?.Text;
        return string.IsNullOrWhiteSpace(text)
            ? "I could not generate a response."
            : text.Trim();
    }

    #endregion
}
