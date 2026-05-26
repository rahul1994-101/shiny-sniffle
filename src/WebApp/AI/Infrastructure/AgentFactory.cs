using Azure;
using Azure.AI.OpenAI;
using Azure.AI.Projects;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using WebApp.AI.Configuration;

namespace WebApp.AI.Infrastructure;

public sealed class AgentFactory(
    FoundryClientFactory clientFactory,
    IOptions<FoundryOptions> options,
    IServiceProvider services)
{
    public AIAgent CreateAgent(string profileKey, IList<AITool>? tools = null)
    {
        var profile = ResolveProfile(profileKey);

        return options.Value.ApiKey is { Length: > 0 } apiKey
            ? CreateWithApiKey(profile, apiKey, tools)
            : CreateWithIdentity(profile, tools);
    }

    public AgentProfileOptions GetProfile(string profileKey) => ResolveProfile(profileKey);

    private AIAgent CreateWithIdentity(AgentProfileOptions profile, IList<AITool>? tools)
    {
        AIProjectClient client = clientFactory.GetIdentityClient();

        return client.AsAIAgent(
            model: profile.ModelDeployment,
            instructions: profile.Instructions,
            name: profile.Name,
            description: profile.Description,
            tools: tools,
            services: services);
    }

    private AIAgent CreateWithApiKey(AgentProfileOptions profile, string apiKey, IList<AITool>? tools)
    {
        var openAiEndpoint = FoundryEndpointHelper.GetOpenAiV1Endpoint(
            options.Value.ProjectEndpoint,
            options.Value.OpenAiEndpoint);

        var chatClient = new AzureOpenAIClient(openAiEndpoint, new AzureKeyCredential(apiKey))
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
