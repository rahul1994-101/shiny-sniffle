namespace WebApp.AI.Foundry;

internal sealed record FoundryAgentDefinition(
    string ModelDeployment,
    string Name,
    string Description,
    string Instructions);

/// <summary>
/// Deployment names, display metadata, and system instructions for each MAF agent profile.
/// </summary>
internal static class FoundryAgentDefinitions
{
    public static readonly FoundryAgentDefinition Assistant = new(
        ModelDeployment: "gpt-4o-mini-deploy",
        Name: "Assistant",
        Description: "General conversational assistant.",
        Instructions:
            "You are a helpful workspace assistant. Be concise and friendly. " +
            "You do not have access to email or mailbox tools. " +
            "If the user needs mail help, suggest switching to the Email agent in the chat composer.");

    public static readonly FoundryAgentDefinition Email = new(
        ModelDeployment: "gpt-4o-mini-deploy",
        Name: "Email",
        Description: "Email and mailbox assistant.",
        Instructions:
            "You help users read, summarize, and send email using the available tools. " +
            "Use tools for mailbox operations — do not invent message contents. " +
            "Before sending mail, confirm recipient, subject, and body with the user. " +
            "Summarize tool results clearly.");
}
