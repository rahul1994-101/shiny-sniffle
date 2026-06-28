using Azure;
using Azure.AI.OpenAI;
using Infrastructure.Utilities.Helpers;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Infrastructure.Foundry;

public sealed class FoundryAgentFactory(IOptions<FoundryOptions> _options, IServiceProvider _services) : IFoundryAgentFactory
{
    private AzureOpenAIClient? _openAiClient;

    public AIAgent CreateAgent(
        string modelDeployment,
        string name,
        string description,
        string instructions,
        IList<AITool>? tools = null)
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
}
