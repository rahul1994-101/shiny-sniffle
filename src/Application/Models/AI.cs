namespace Application.Models;

public sealed class RunChatAgentRequest
{
    public Guid UserId { get; init; }

    public Guid ThreadId { get; init; }

    public ChatAgent ChatAgent { get; init; }

    /// <summary>Resolved entity details for @mentions in the latest user turn (not persisted).</summary>
    public string? MentionContext { get; init; }

    /// <summary>
    /// Resolved mailbox alias from an <c>@mailbox:alias</c> mention in the latest user turn.
    /// Applied when mailbox tools omit <c>mailbox_alias</c>.
    /// </summary>
    public string? MailboxAlias { get; init; }
}

public sealed class RunChatAgentResponse
{
    public string AssistantContent { get; init; } = string.Empty;
}
