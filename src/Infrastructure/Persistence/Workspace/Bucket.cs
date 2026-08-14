namespace Infrastructure.Persistence.Workspace;

/// <summary>
/// Row in <c>workspace.Bucket</c> — user-owned referable group (bucket:{alias}).
/// Groups contacts and mailboxes via <see cref="BucketAssignment"/>.
/// </summary>
public class Bucket : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>UI-only (e.g. #RRGGBB).</summary>
    public string? Color { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Per-user handle (NOT NULL in DB); optional in UI; auto-generated from name when blank on save.</summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>Optional facts for UI, rules, and agent prompts.</summary>
    public string? Context { get; set; }
}
