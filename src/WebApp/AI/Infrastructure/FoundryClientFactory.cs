using Azure.AI.Projects;
using Azure.Identity;

using Microsoft.Extensions.Options;

using WebApp.AI.Configuration;

namespace WebApp.AI.Infrastructure;

public sealed class FoundryClientFactory(IOptions<FoundryOptions> options)
{
    private AIProjectClient? _identityClient;

    public bool IsConfigured =>
        options.Value.Enabled &&
        !string.IsNullOrWhiteSpace(options.Value.ProjectEndpoint);

    public bool UsesApiKey => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public AIProjectClient GetIdentityClient()
    {
        if (UsesApiKey)
        {
            throw new InvalidOperationException(
                "Foundry API key auth does not use AIProjectClient. Use AgentFactory instead.");
        }

        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Foundry is not configured. Set Foundry:Enabled and Foundry:ProjectEndpoint.");
        }

        return _identityClient ??= new AIProjectClient(
            new Uri(options.Value.ProjectEndpoint),
            new DefaultAzureCredential());
    }
}
