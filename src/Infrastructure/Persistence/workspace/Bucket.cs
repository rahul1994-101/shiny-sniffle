namespace Infrastructure.Persistence.workspace;

public class Bucket : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
