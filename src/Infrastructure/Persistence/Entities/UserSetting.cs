namespace Infrastructure.Persistence.Entities;

/// <summary>Per-user workspace settings row (one active row per user). Mail credentials live in <see cref="EmailAccount"/>.</summary>
public class UserSetting : BaseAuditableEntity
{
    public Guid UserId { get; set; }
}
