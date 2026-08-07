namespace Infrastructure.Persistence.Entities;

/// <summary>
/// Row in <c>dbo.EmailAccount</c> — user-connected external inbox (Settings → Email → Accounts).
/// Not the same as <see cref="User.Email"/> (app login).
/// </summary>
public class EmailAccount : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public Guid EmailProviderId { get; set; }

    /// <summary>User-defined label (e.g. Work, Primary).</summary>
    public string Alias { get; set; } = string.Empty;

    public string EmailAddress { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public int SortOrder { get; set; }

    public EmailProviderDefinition? EmailProvider { get; set; }
}
