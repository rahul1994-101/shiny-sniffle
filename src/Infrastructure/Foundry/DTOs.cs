namespace Infrastructure.Foundry;

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
