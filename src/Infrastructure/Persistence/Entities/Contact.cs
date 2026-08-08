namespace Infrastructure.Persistence.Entities;

/// <summary>
/// Row in <c>workspace.Contact</c> — user-owned reference person for workflows and features.
/// </summary>
public class Contact : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public ContactSource Source { get; set; } = ContactSource.Manual;

    public int SortOrder { get; set; }
}
