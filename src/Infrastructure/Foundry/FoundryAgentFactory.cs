using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Infrastructure.Foundry;

public sealed class FoundryAgentFactory(IOptions<FoundryOptions> _options, IServiceProvider _services) : IFoundryAgentFactory
{
    private readonly Lazy<AzureOpenAIClient> _openAiClient = new(() =>
    {
        var foundry = _options.Value;
        var endpoint = AzureOpenAiEndpointHelpers.ToAzureOpenAiBaseUri(foundry.Endpoint);
        var credential = new AzureKeyCredential(foundry.ApiKey);
        var clientOptions = AzureOpenAiEndpointHelpers.CreateClientOptions(foundry.ApiVersion);

        return clientOptions is null
            ? new AzureOpenAIClient(endpoint, credential)
            : new AzureOpenAIClient(endpoint, credential, clientOptions);
    });

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

        var chatClient = _openAiClient.Value
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
}
