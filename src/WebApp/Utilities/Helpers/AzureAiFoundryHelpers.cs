using Azure;
using Azure.AI.OpenAI;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using WebApp.AI.Agents;
using WebApp.AI.Agents.Chat;
using WebApp.AI.Agents.Intent;
using WebApp.AI.Memory;
using WebApp.AI.Orchestration;

namespace WebApp.Utilities.Helpers;

public static class AzureAiFoundryHelpers
{
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FoundryOptions>(configuration.GetSection(FoundryOptions.SectionName));
        services.AddSingleton<FoundryAgentFactory>();

        services.AddScoped<ThreadMemoryProvider>();
        services.AddScoped<IntentClassificationAgent>();
        services.AddScoped<GeneralChatAgent>();
        services.AddScoped<IntentRouter>();
        services.AddScoped<ChatOrchestrator>();

        return services;
    }

    public static Uri GetAzureOpenAiEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("Foundry:Endpoint is not configured.");
        }

        var trimmed = endpoint.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Foundry:Endpoint is not a valid absolute URL.");
        }

        var baseUrl = ExtractBaseUrl(uri);
        return NormalizeTrailingSlash(baseUrl);
    }

    private static string ExtractBaseUrl(Uri uri)
    {
        var path = uri.AbsolutePath;

        var deploymentsIndex = path.IndexOf("/openai/deployments/", StringComparison.OrdinalIgnoreCase);
        if (deploymentsIndex >= 0)
        {
            return $"{uri.Scheme}://{uri.Authority}{path[..deploymentsIndex]}/";
        }

        var openAiIndex = path.IndexOf("/openai/", StringComparison.OrdinalIgnoreCase);
        if (openAiIndex >= 0)
        {
            return $"{uri.Scheme}://{uri.Authority}{path[..openAiIndex]}/";
        }

        return $"{uri.Scheme}://{uri.Authority}/";
    }

    private static Uri NormalizeTrailingSlash(string endpoint)
    {
        var trimmed = endpoint.Trim();
        if (!trimmed.EndsWith('/'))
        {
            trimmed += "/";
        }

        return new Uri(trimmed, UriKind.Absolute);
    }
}

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

public sealed class FoundryAgentFactory(IOptions<FoundryOptions> options, IServiceProvider services)
{
    private AzureOpenAIClient? _openAiClient;

    public AIAgent CreateAgent(string profileKey, IList<AITool>? tools = null)
    {
        var foundry = options.Value;
        if (!foundry.IsConfigured)
        {
            throw new InvalidOperationException(
                "Foundry is not configured. Set Foundry:Enabled, Foundry:Endpoint, and Foundry:ApiKey.");
        }

        var profile = AgentProfiles.Get(profileKey);
        var chatClient = GetOpenAiClient(foundry)
            .GetChatClient(profile.ModelDeployment)
            .AsIChatClient();

        return new ChatClientAgent(
            chatClient,
            profile.Instructions,
            profile.Name,
            profile.Description,
            tools,
            services: services);
    }

    public AgentProfile GetProfile(string profileKey) => AgentProfiles.Get(profileKey);

    private AzureOpenAIClient GetOpenAiClient(FoundryOptions foundry)
    {
        if (_openAiClient is not null)
        {
            return _openAiClient;
        }

        var endpoint = AzureAiFoundryHelpers.GetAzureOpenAiEndpoint(foundry.Endpoint);

        return _openAiClient = new AzureOpenAIClient(
            endpoint,
            new AzureKeyCredential(foundry.ApiKey));
    }
}
