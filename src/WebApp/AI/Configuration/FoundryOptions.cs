namespace WebApp.AI.Configuration;

public sealed class FoundryOptions
{
    public const string SectionName = "Foundry";

    public bool Enabled { get; set; }

    public string ProjectEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Optional Foundry API key (Plesk, user secrets). When set, used instead of Azure identity (az login / MI).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional override for the OpenAI-compatible Foundry URL (defaults to {resource}/openai/v1/ derived from ProjectEndpoint).
    /// </summary>
    public string OpenAiEndpoint { get; set; } = string.Empty;

    public Dictionary<string, AgentProfileOptions> Profiles { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AgentProfileOptions
{
    public string ModelDeployment { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;
}

public static class AgentProfileKeys
{
    public const string IntentRouter = "intent-router";
    public const string ChatGeneral = "chat-general";
    public const string WorkspaceData = "workspace-data";
    public const string WorkspacePresenter = "workspace-presenter";
}
