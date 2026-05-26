using Azure;
using Azure.AI.OpenAI;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using WebApp.AI.Configuration;

namespace WebApp.AI.Infrastructure;

public sealed class AgentFactory(IOptions<FoundryOptions> options, IServiceProvider services)
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

        var profile = ResolveProfile(profileKey);
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

    public AgentProfileOptions GetProfile(string profileKey) => ResolveProfile(profileKey);

    private AzureOpenAIClient GetOpenAiClient(FoundryOptions foundry)
    {
        if (_openAiClient is not null)
        {
            return _openAiClient;
        }

        var endpoint = FoundryEndpointHelper.GetAzureOpenAiEndpoint(foundry.Endpoint);

        return _openAiClient = new AzureOpenAIClient(
            endpoint,
            new AzureKeyCredential(foundry.ApiKey));
    }

    private AgentProfileOptions ResolveProfile(string profileKey)
    {
        if (!options.Value.Profiles.TryGetValue(profileKey, out var profile) ||
            string.IsNullOrWhiteSpace(profile.ModelDeployment) ||
            string.IsNullOrWhiteSpace(profile.Instructions))
        {
            throw new InvalidOperationException(
                $"Foundry agent profile '{profileKey}' is missing or incomplete in configuration.");
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            profile.Name = profileKey;
        }

        return profile;
    }
}
