using Azure;
using Azure.AI.OpenAI;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using WebApp.AI.Agents;
using WebApp.Models;

namespace WebApp.AI.Foundry;

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

        var endpoint = FoundryExtensions.GetAzureOpenAiEndpoint(foundry.Endpoint);

        return _openAiClient = new AzureOpenAIClient(
            endpoint,
            new AzureKeyCredential(foundry.ApiKey));
    }
}
