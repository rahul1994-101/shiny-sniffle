namespace Infrastructure.Foundry;

#region # Options

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

    public bool IsConfigured =>
        Enabled &&
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(ApiKey);
}

#endregion

#region # Deployments

/// <summary>Azure Foundry model deployment names ({model}-deploy).</summary>
public static class FoundryDeployments
{
    public const string Gpt4oMini = "gpt-4o-mini-deploy";

    public const string Gpt54 = "gpt-5.4-deploy";

    public const string Gpt54Nano = "gpt-5.4-nano-deploy";

    public const string Gpt54Mini = "gpt-5.4-mini-deploy";
}

#endregion
