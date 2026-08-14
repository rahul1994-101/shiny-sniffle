namespace Application.Models;

public sealed class RunChatAgentRequest
{
    public Guid UserId { get; init; }

    public Guid ThreadId { get; init; }

    public ChatAgent ChatAgent { get; init; }
}

public sealed class RunChatAgentResponse
{
    public string AssistantContent { get; init; } = string.Empty;
}
