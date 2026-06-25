namespace Core.Entities;

public abstract class BaseEntity
{
    // Primary key with auto-generated sequential UUID
    public Guid Id { get; set; }

    // Status and lifecycle management
    public bool IsActive { get; set; } = true;
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
