namespace Application.Models;

public sealed class RunChatAgentRequest
{
    public Guid UserId { get; init; }

    public Guid ThreadId { get; init; }

    public ChatAgent ChatAgent { get; init; }

    /// <summary>Resolved entity details for @mentions in the latest user turn (not persisted).</summary>
    public string? MentionContext { get; init; }
}

public sealed class RunChatAgentResponse
{
    public string AssistantContent { get; init; } = string.Empty;
}
