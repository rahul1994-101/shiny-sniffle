namespace Application.Features.Chat.ChatThreads;

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

    internal static ChatAgent ToModel(ChatThreadAgent agent) => (ChatAgent)(int)agent;

    internal static ChatThreadAgent ToPersistence(ChatAgent agent) => (ChatThreadAgent)(int)agent;
}
