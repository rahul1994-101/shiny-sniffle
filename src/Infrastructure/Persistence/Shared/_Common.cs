namespace Infrastructure.Persistence.Shared;

public abstract class BaseEntity
{
    // Primary key with auto-generated sequential UUID
    public Guid Id { get; set; }

    /// <summary>Paused by user; reversible. Reads filter <c>IsActive &amp;&amp; !IsDeleted</c>.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Removed by user; hidden from UI; row retained internally.</summary>
    public bool IsDeleted { get; set; }
}

public abstract class BaseAuditableEntity : BaseEntity
{
    // Audit fields for tracking changes
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
