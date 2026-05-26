namespace WebApp.AI.Configuration;

public sealed class FoundryOptions
{
    public const string SectionName = "Foundry";

    public bool Enabled { get; set; }

    /// <summary>
    /// Azure OpenAI resource base URL (maps to AZURE_OPENAI_ENDPOINT without deployment path).
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API key (maps to AZURE_OPENAI_API_KEY).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional API version override (maps to AZURE_OPENAI_API_VERSION).
    /// </summary>
    public string ApiVersion { get; set; } = string.Empty;

    public Dictionary<string, AgentProfileOptions> Profiles { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public bool IsConfigured =>
        Enabled &&
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(ApiKey);
}

public sealed class AgentProfileOptions
{
    /// <summary>
    /// Model deployment name (maps to AZURE_OPENAI_DEPLOYMENT_NAME per profile).
    /// </summary>
    public string ModelDeployment { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;
}

public static class AgentProfileKeys
{
    public const string IntentRouter = "intent-router";
    public const string ChatGeneral = "chat-general";
}
