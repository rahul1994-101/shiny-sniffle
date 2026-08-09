namespace Infrastructure.Persistence.Entities;

/// <summary>
/// Row in <c>workspace.Contact</c> — user-owned reference person for workflows and features.
/// </summary>
public class Contact : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>Per-user handle (NOT NULL in DB); optional in UI; auto-generated from name when blank on save.</summary>
    public string Alias { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    /// <summary>Creation provenance (<see cref="ContactSource"/>). Set on insert by the feature that creates the row.</summary>
    public ContactSource Source { get; set; } = ContactSource.Manual;

    public int SortOrder { get; set; }
}
