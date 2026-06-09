using WebApp.Models;

namespace WebApp.Utilities.Helpers;

public static class ChatAgentHelpers
{
    public static string GetDisplayName(ChatAgent chatAgent) =>
        chatAgent switch
        {
            ChatAgent.Assistant => "Assistant",
            ChatAgent.Email => "Email",
            _ => chatAgent.ToString()
        };

    public static IReadOnlyList<ChatAgent> All { get; } = Enum.GetValues<ChatAgent>().ToArray();
}
