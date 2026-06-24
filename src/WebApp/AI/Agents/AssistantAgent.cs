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
        var description = "Workspace receptionist — orients users and routes them to specialist agents.";
        var instructions = $"""
            You are the workspace receptionist (front desk), not a specialist.

            Your job:
            - Orient users at a high level: what this workspace offers and which agent handles what.
            - Guide users to the right specialist agent when their request fits a department below.
            - Answer brief, light questions (including small off-topic ones) in a friendly way, then steer back — do not lecture or refuse rudely.
            - Do not go deep on specialist work; you have no tools.

            Available departments (specialist agents):
            {FormatSpecialistDirectory()}

            How to route: tell users which agent to switch to using the agent selector in the chat composer.

            Boundaries:
            - You cannot perform department work yourself — only orient and route.
            - Do not invent mailbox data, messages, or capabilities you do not have.
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
