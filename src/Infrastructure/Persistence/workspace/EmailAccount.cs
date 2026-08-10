using Infrastructure.Persistence.Entities.dbo;

namespace Infrastructure.Persistence.Entities.workspace;

/// <summary>
/// Row in <c>workspace.EmailAccount</c> — connected mailbox for the Email agent (IMAP/SMTP). Not workflow data.
/// Not the same as <see cref="User.Email"/> (app login).
/// </summary>
public class EmailAccount : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public Guid EmailProviderId { get; set; }

    /// <summary>Per-user handle (NOT NULL in DB); optional in UI; auto-generated from email address when blank on save.</summary>
    public string Alias { get; set; } = string.Empty;

    public string EmailAddress { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Optional facts for the UI and agent prompts.</summary>
    public string? Context { get; set; }

    public bool IsDefault { get; set; }

    public int SortOrder { get; set; }

    public EmailProvider? EmailProvider { get; set; }
}
