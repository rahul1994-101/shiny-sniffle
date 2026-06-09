namespace WebApp.AI.Agents;

public static class AgentProfileKeys
{
    public const string Assistant = "assistant";
    public const string Email = "email";
}

public sealed record AgentProfile(
    string ModelDeployment,
    string Name,
    string Description,
    string Instructions);

public static class AgentProfiles
{
    private static readonly Dictionary<string, AgentProfile> Profiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [AgentProfileKeys.Assistant] = new AgentProfile(
                ModelDeployment: "gpt-4o-mini-deploy",
                Name: "Assistant",
                Description: "General conversational assistant.",
                Instructions:
                    "You are a helpful workspace assistant. Be concise and friendly. " +
                    "You do not have access to email or mailbox tools. " +
                    "If the user needs mail help, suggest switching to the Email agent in the chat composer."),

            [AgentProfileKeys.Email] = new AgentProfile(
                ModelDeployment: "gpt-4o-mini-deploy",
                Name: "Email",
                Description: "Email and mailbox assistant.",
                Instructions:
                    "You help users read, summarize, and send email using the available tools. " +
                    "Use tools for mailbox operations — do not invent message contents. " +
                    "Before sending mail, confirm recipient, subject, and body with the user. " +
                    "Summarize tool results clearly.")
        };

    public static AgentProfile Get(string profileKey)
    {
        if (!Profiles.TryGetValue(profileKey, out var profile))
        {
            throw new InvalidOperationException($"Agent profile '{profileKey}' is not defined.");
        }

        return profile;
    }
}
