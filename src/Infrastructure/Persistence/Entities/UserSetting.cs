namespace Infrastructure.Persistence.Entities;

/// <summary>Per-user app preferences row (Settings UI; one active row per user). Login: <see cref="User"/>. Mail: <see cref="EmailAccount"/>. Contacts/tags: workspace schema.</summary>
public class UserSetting : BaseAuditableEntity
{
    public Guid UserId { get; set; }
}
