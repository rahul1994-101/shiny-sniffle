using Azure;
using Azure.AI.OpenAI;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using WebApp.Models;

namespace WebApp.AI.Foundry;

public sealed class FoundryAgentFactory(IOptions<FoundryOptions> options, IServiceProvider services)
{
    private AzureOpenAIClient? _openAiClient;

    public AIAgent CreateAssistantAgent() =>
        CreateAgent(FoundryAgentDefinitions.Assistant);

    public AIAgent CreateEmailAgent(IList<AITool>? tools = null) =>
        CreateAgent(FoundryAgentDefinitions.Email, tools);


    #region # Private Helpers

    private AIAgent CreateAgent(FoundryAgentDefinition definition, IList<AITool>? tools = null)
    {
        var foundry = options.Value;
        if (!foundry.IsConfigured)
        {
            throw new InvalidOperationException(
                "Foundry is not configured. Set Foundry:Enabled, Foundry:Endpoint, and Foundry:ApiKey.");
        }

        var chatClient = GetOpenAiClient(foundry)
            .GetChatClient(definition.ModelDeployment)
            .AsIChatClient();

        return new ChatClientAgent(
            chatClient,
            definition.Instructions,
            definition.Name,
            definition.Description,
            tools,
            services: services);
    }

    private AzureOpenAIClient GetOpenAiClient(FoundryOptions foundry)
    {
        if (_openAiClient is not null)
        {
            return _openAiClient;
        }

        var endpoint = GetAzureOpenAiEndpoint(foundry.Endpoint);

        return _openAiClient = new AzureOpenAIClient(
            endpoint,
            new AzureKeyCredential(foundry.ApiKey));
    }

    private static Uri GetAzureOpenAiEndpoint(string endpoint)
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

        return NormalizeTrailingSlash(ExtractBaseUrl(uri));
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

    #endregion
}
