namespace Infrastructure.Persistence.workspace;

public class Tag : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>UI-only (e.g. #RRGGBB). Not used by AI.</summary>
    public string? Color { get; set; }

    public int SortOrder { get; set; }
}
