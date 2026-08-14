namespace Infrastructure.Persistence.Dbo;

/// <summary>Per-user app preferences row (Settings UI; one active row per user). Login: <see cref="User"/>. Mail and contacts: workspace schema.</summary>
public class UserSetting : BaseAuditableEntity
{
    public Guid UserId { get; set; }
}
