using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Infrastructure.Foundry;

public interface IFoundryAgentFactory
{
    AIAgent CreateAgent(
        string modelDeployment,
        string name,
        string description,
        string instructions,
        IList<AITool>? tools = null);
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
