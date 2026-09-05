using Application.Features.Workspace.EmailAccounts;

namespace Application.Models;

public sealed class RunChatAgentRequest
{
    public Guid UserId { get; init; }

    public Guid ThreadId { get; init; }

    public ChatAgent ChatAgent { get; init; }

    /// <summary>Resolved entity details for @mentions in the latest user turn (not persisted).</summary>
    public string? MentionContext { get; init; }

    /// <summary>Pre-resolved default mailbox for this turn (mention or default account).</summary>
    public MailboxAccountContext? DefaultMailboxAccount { get; init; }

    /// <summary>True when more than one mailbox was mentioned — tools must receive <c>mailbox_alias</c>.</summary>
    public bool RequireMailboxAlias { get; init; }
}

public sealed class RunChatAgentResponse
{
    public string AssistantContent { get; init; } = string.Empty;
}
