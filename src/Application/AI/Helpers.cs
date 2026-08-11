using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Application.AI;

internal static class AgentResponseHelpers
{
    internal static string ExtractAssistantText(AgentResponse response)
    {
        var text = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text;
        return string.IsNullOrWhiteSpace(text)
            ? "I could not generate a response."
            : text.Trim();
    }
}
