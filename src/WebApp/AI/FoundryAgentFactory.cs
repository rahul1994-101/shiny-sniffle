using Azure;
using Azure.AI.OpenAI;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using WebApp.Models;
using WebApp.Utilities.Helpers;

namespace WebApp.AI;

public sealed class FoundryAgentFactory(IOptions<FoundryOptions> _options, IServiceProvider _services)
{
    private AzureOpenAIClient? _openAiClient;

    public AIAgent CreateAgent(string modelDeployment, string name, string description, string instructions, IList<AITool>? tools = null)
    {
        var foundry = _options.Value;
        if (!foundry.IsConfigured)
        {
            throw new InvalidOperationException(
                "Foundry is not configured. Set Foundry:Enabled, Foundry:Endpoint, and Foundry:ApiKey.");
        }

        var chatClient = GetOpenAiClient(foundry)
            .GetChatClient(modelDeployment)
            .AsIChatClient();

        return new ChatClientAgent(
            chatClient,
            instructions,
            name,
            description,
            tools,
            services: _services);
    }

    #region # Private Helpers

    private AzureOpenAIClient GetOpenAiClient(FoundryOptions foundry)
    {
        if (_openAiClient is not null)
        {
            return _openAiClient;
        }

        var endpoint = AzureOpenAiEndpointHelpers.ToAzureOpenAiBaseUri(foundry.Endpoint);

        return _openAiClient = new AzureOpenAIClient(
            endpoint,
            new AzureKeyCredential(foundry.ApiKey));
    }

    #endregion
}

/// <summary>
/// Azure Foundry model deployment names ({model}-deploy).
/// </summary>
public static class FoundryDeployments
{
    public const string Gpt4oMini = "gpt-4o-mini-deploy";
    public const string Gpt54 = "gpt-5.4-deploy";
    public const string Gpt54Nano = "gpt-5.4-nano-deploy";
    public const string Gpt54Mini = "gpt-5.4-mini-deploy";
}
