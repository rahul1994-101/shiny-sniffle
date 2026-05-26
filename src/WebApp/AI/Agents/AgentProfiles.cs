namespace WebApp.AI.Agents;

public static class AgentProfileKeys
{
    public const string IntentRouter = "intent-router";
    public const string ChatGeneral = "chat-general";
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
            [AgentProfileKeys.IntentRouter] = new AgentProfile(
                ModelDeployment: "gpt-4o-mini-deploy",
                Name: "IntentRouter",
                Description: "Classifies user intent for routing.",
                Instructions:
                    "You classify user messages for a workspace assistant. " +
                    "Return JSON with intent, confidence (0-1), and reason. " +
                    "Use general.chat for all messages for now. Do not answer the user."),

            [AgentProfileKeys.ChatGeneral] = new AgentProfile(
                ModelDeployment: "gpt-4o-mini-deploy",
                Name: "GeneralAssistant",
                Description: "General conversational assistant.",
                Instructions: "You are a helpful workspace assistant. Be concise and friendly.")
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
