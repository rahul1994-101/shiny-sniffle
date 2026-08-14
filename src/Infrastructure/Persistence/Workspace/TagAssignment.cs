namespace Infrastructure.Persistence.Workspace;

public class TagAssignment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid TagId { get; set; }

    public ReferableKind ReferableKind { get; set; }

    public Guid ReferableId { get; set; }

    public Tag? Tag { get; set; }
}
