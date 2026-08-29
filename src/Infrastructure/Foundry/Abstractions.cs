using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Infrastructure.Foundry;

public interface IFoundryAgentFactory
{
    AIAgent CreateAgent(string modelDeployment, string name, string description, string instructions, IList<AITool>? tools = null);
}
