using System.Text;

using Microsoft.Agents.AI;

using WebApp.AI;
using WebApp.Models;

namespace WebApp.AI.Agents;

public sealed class AssistantAgent(FoundryAgentFactory _agentFactory)
{
    public async Task<RunChatAgentResponse> RunAsync(RunChatAgentRequest request, IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> history, CancellationToken cancellationToken = default)
    {
        #region # Execute

        var agent = CreateAssistantAgent();
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

    private AIAgent CreateAssistantAgent()
    {
        var modelDeployment = FoundryDeployments.Gpt4oMini;
        var name = "Assistant";
        var description = "Workspace receptionist that explains this app's agents and routes users to the right specialist.";
        var instructions = $"""
            You are the workspace receptionist (front desk), not a specialist.

            Your job:
            - Orient users at a high level: what this workspace offers and which listed agent handles what.
            - Guide users to the right specialist agent when their request fits one of the available departments.
            - Answer brief questions only when they are about this workspace, available agents, settings, navigation, or how to use the app.
            - Do not perform specialist work yourself; you have no tools.

            Available departments:
            {FormatSpecialistDirectory()}

            Routing rules:
            - If the request matches a listed department, tell the user:
              "This is handled by the [Agent Name] agent. Please switch to [Agent Name] using the agent selector in the chat composer."
            - If the request is ambiguous between listed departments only, ask one brief clarifying question to choose the right agent.
            - If no listed department handles the request, say this workspace does not currently have an agent for that task.
            - Only route to agents listed in Available departments. Do not invent agents, tools, mailbox data, or capabilities.

            Boundaries:
            - Stay on topic: this app and its agents only.
            - Do not answer general knowledge, coding help, trivia, chit-chat, or other off-topic requests.
            - For off-topic requests, decline briefly and redirect to a workspace-related question or the right listed agent.
            - Keep replies concise, warm, and professional.
            """;

        return _agentFactory.CreateAgent(modelDeployment, name, description, instructions);
    }

    private static string FormatSpecialistDirectory()
    {
        #region # Types

        // SpecialistRoute: (DisplayName, Summary)

        #endregion

        #region # Routes

        // Add an entry when a new specialist agent ships.
        (string DisplayName, string Summary)[] specialistRoutes =
        [
            ("Email", "Read, summarize, and send email from the connected mailbox.")
        ];

        #endregion

        #region # Format

        var builder = new StringBuilder();

        for (var i = 0; i < specialistRoutes.Length; i++)
        {
            var route = specialistRoutes[i];
            builder.Append("- ");
            builder.Append(route.DisplayName);
            builder.Append(": ");
            builder.Append(route.Summary);

            if (i < specialistRoutes.Length - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();

        #endregion
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
