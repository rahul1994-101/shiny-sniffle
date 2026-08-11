namespace Infrastructure.Persistence.workspace;

public class BucketAssignment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid BucketId { get; set; }

    public ReferableKind ReferableKind { get; set; }

    public Guid ReferableId { get; set; }

    public Bucket? Bucket { get; set; }
}
